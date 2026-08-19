# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

An emulator for a custom 12-bit CPU (4096 words of RAM, 16 registers), plus a GUI that shows
RAM/register contents live while a program runs. `README.md` holds the authoritative-ish
instruction-set table; see "README vs. implementation" below for known drift.

**This repo is mid-migration** from WPF on .NET Framework 4.7.2 to Avalonia 12 on .NET 10.
Phases 1–3 are done and the WPF app has been deleted. The emulator core lives in
`src/CpuEmulator.Core`, builds and runs on Linux, no longer touches the console, the process or
the filesystem, and is covered by 57 tests in `tests/CpuEmulator.Tests`. **There is no UI project
right now** — the Avalonia app arrives in Phase 4. The old WPF sources are recoverable
from git history if the port needs them:

```
git show main:MainWindow.xaml      # and App.xaml, MainWindow.xaml.cs, App.xaml.cs
```

## Build & run

.NET 10, SDK-style projects, cross-platform. Builds on Linux:

```
dotnet build                                    # solution: CpuEmulator.sln (classic .sln, not .slnx, for VS compat)
dotnet test                                     # 57 tests, ~70ms
dotnet test --filter FullyQualifiedName~AluTests            # one class
dotnet test --filter FullyQualifiedName~Sub_ComputesAMinusB # one test
```

`src/CpuEmulator.Core` is a class library with no UI dependencies and no entry point, so there is
nothing to `dotnet run` yet — the Avalonia app arrives in Phase 4. To exercise the CPU today, add a
throwaway console project referencing the Core project.

There is no linter. `.github/workflows/dotnet-desktop.yml` is unmodified GitHub boilerplate — its
`Solution_Name`/`Test_Project_Path`/WAP-packaging env vars are still placeholder strings, so the
workflow does not build this project. It gets replaced in Phase 5; don't treat it as a signal of
how the project is built.

## Architecture

Two source files carry all the logic:

- `src/CpuEmulator.Core/Data12Bit.cs` — a **struct** wrapping a `uint` whose setter masks with `0xfff`. All 12-bit
  wraparound in the emulator comes from this one mask; nothing else truncates. Because it is a
  value type, `Data12Bit` copies are snapshots, and mutation always goes through `ref` parameters
  or direct array-element access (`RAM[i].Val = ...`, `CPU_Move(ref REG[b], ...)`).
- `src/CpuEmulator.Core/Emulator.cs` — the CPU itself. State is `RAM[4096]` and `REG[16]`, both public so the UI can read
  them.

There is no UI layer in the tree at present; the section below records how the deleted WPF shell
worked, because Phase 4 has to reproduce its behavior.

### Driving the core

The core is a plain library with no ambient dependencies. A host wires it up like this:

```csharp
var cpu = new Emulator(inputSource);              // inputSource optional
cpu.LoadProgram(ProgramLoader.ParseFile(path));   // or ProgramLoader.Parse(lines)
while (cpu.IsRunning())
    cpu.NextCommand(cancellationToken);
cpu.Reset();                                      // makes it runnable again
```

Four seams exist so the CPU can be tested and hosted without a console (added in Phase 2):

- **`ProgramLoader`** — `Parse` is pure over lines; `ParseFile` is the only file I/O in the core.
  `Emulator.LoadProgram` now takes already-parsed words, not a path.
- **`IInputSource`** — supplies the user-input instruction (L1 opcode 7). Prompting, validation
  and retry are the *host's* job; the CPU just blocks on `ReadValue`. Constructing an `Emulator`
  without one and then running a program that uses the instruction throws rather than hanging.
- **`InvalidOpcodeException`** — carries the decode level, address and raw word. Replaces
  `Environment.Exit(1|2|3)`, which used to kill the host process silently.
- **`ProgramFormatException`** — carries the 1-based line number of a bad listing line.

`DumpRam()`, `DumpRegisters()` and `DumpFlags()` return strings (they used to write to `Console`),
which makes them usable in test failure messages.

The `cancellationToken` on `NextCommand` is observed *only* while blocked on user input — a host
stopping a running CPU otherwise just stops calling the method. This is what Phase 4 needs so the
Stop button can interrupt a CPU parked on input.

### Instruction decoding

`NextCommand()` reads `RAM[CounterReg]`, splits the 12-bit word into three nibbles
(`instruction = op & 0xf`, `arg_a = (op >> 4) & 0xf`, `arg_b = (op >> 8) & 0xf`), increments the
counter, then dispatches. Nibble `0` is an escape into a deeper decode level, which is why the
opcode table in the README is three tiers:

- `ExecuteCommand_L0(instruction, arg_a, arg_b)` — both args are register addresses.
- `ExecuteCommand_L1(arg_a, arg_b)` — reached when `instruction == 0`; `arg_a` is now the opcode.
  These are the immediate/unary forms.
- `ExecuteCommand_L2(arg_b)` — reached when `instruction == 0 && arg_a == 0`; nullary ops
  (stop, conditional stop).

Immediate operands are the **next word in RAM**: the L1 handlers do `CounterReg++` and then read
`RAM[CounterReg - 1]`, so an immediate instruction occupies two words. Program files reflect this
(`110` followed by `030` = "Mov 0x030, REG[1]").

Invalid opcodes call `Environment.Exit(1|2|3)` — the exit code identifies which decode level
rejected it. This kills the whole GUI process, not just the emulator thread.

### Special registers and flags

Three registers are architectural, addressed like any other:

- `REG[15]` — program counter (`CounterReg`)
- `REG[14]` — flag register (`FlagReg`), bitfield of `Flags` (AGreater/BGreater/Equal/Overflow)
- `REG[13]` — condition mask (`CheckFlagReg`)

Conditional instructions call `CPU_CheckCondition()`, which ANDs `FlagReg` with `CheckFlagReg` and
takes any nonzero result as true. So a program sets up a branch by first writing the flags it cares
about into `REG[13]`, then executing the conditional op. Flags are only ever *set* (`Set_Flag` ORs);
programs clear them by moving `0` into `REG[14]` themselves.

Jumps are not a distinct instruction — writing to `REG[15]` is the jump.

### How the deleted WPF shell worked (Phase 4 must replace this)

`Start_CPU` ran `EmulatorUpdate()` on a background `Thread` spinning `NextCommand()` with a 1 ms
sleep per instruction; a `DispatcherTimer` polled every 10 ms and rebuilt every row object from
scratch (4096 + 16 per tick) because `Data12Bit` values are copied, not bound. `Next_Tick` stepped
one instruction on the UI thread.

Three defects the replacement must not inherit:

- `Stop_CPU` used `Thread.Abort()`, which **throws `PlatformNotSupportedException` on .NET 10**.
  Needs cooperative `CancellationToken` cancellation — and that token must reach the user-input
  wait, or Stop will hang exactly when it matters.
- The `Emulator` instance was never reset, so it could not be restarted after a program halted
  (`_isCpuRunning` is never set back to true). The core needs a `Reset()`.
- Rebuilding 4,112 row objects every 10 ms is wasteful; Avalonia's `DataGrid` virtualizes, so use
  `INotifyPropertyChanged` rows mutated in place instead.

The `programerData` grid, its Up/Down/Write buttons, and the three `ListBox`es were unwired
placeholders — a program editor that never existed. Porting them is a feature, not a migration.

## Tests

`tests/CpuEmulator.Tests` (xUnit). The data files in `data/` are linked into the test output, so
tests run against the same programs the app ships rather than against copies that can drift.

The regression net has two layers:

- **Golden tests** (`GoldenProgramTests`) run the real programs end to end. `program.txt` must halt
  after **exactly 1655 instructions**, and `program1.txt` after **16323** with a scripted input.
  `Golden/program.ram.txt` is a full 4096-word RAM snapshot compared word for word — the strongest
  assertion available, and the thing that makes it safe to rewrite the UI.
- **Unit tests** pin the sharp edges an unwary refactor would smooth over: subtraction operand
  order, overflow computed on the unmasked `uint`, the `REG[13] & REG[14]` condition mask,
  immediates consuming the following word, and flags being set-only.

To regenerate the golden snapshot after a *deliberate* behavior change, run `program.txt` to a halt
and write `emulator.DumpRam()` over `Golden/program.ram.txt`. Comparison is line-ending-insensitive
(`Cpu.Normalize`) so it holds on Linux and Windows alike.

`Cpu.cs` holds the helpers: `Cpu.W(argB, argA, instruction)` assembles a word, `Cpu.L1`/`Cpu.L2`
assemble the deeper decode tiers, and `ScriptedInput`/`BlockingInput` stand in for `IInputSource`.

## Program files

`data/*.txt`: one 3-hex-digit word per line, `//`-prefixed lines are comments (existing comments are
Polish; `program.txt` is CP1250 with CRLF endings, `program1.txt` is UTF-8 — Phase 5 re-encodes).
`ProgramLoader.Parse(IEnumerable<string>)` is a pure function and is what tests should use;
`ProgramLoader.ParseFile(path)` is the only file-touching code in the core and resolves paths
against the working directory like any normal program. Phase 5 marks the data files as
`CopyToOutputDirectory`.

Loading does not clear RAM first, and there is no assembler — programs are hand-assembled hex.
`program.txt` is a 64-word bubble sort with no user-input opcode, so it runs unattended to a halt
(1655 instructions, data at `0x30..0x3F` sorted ascending) — this is the Phase 3 golden test.
`program1.txt` uses opcode `270` (user input into `REG[2]`) and needs a scripted input source.

## README vs. implementation

The README table has drifted from `Emulator.cs`; trust the code and fix the README when you touch it:

- L0 opcodes `E` and `F` are swapped relative to the table — in code, `14` is
  `Mov RAM[REG[a]], REG[b]` (store) and `15` is `Mov REG[b], RAM[REG[a]]` (load).
- "Clear Flags" (`2,0,0`) is documented but not implemented in `ExecuteCommand_L2`.
- `ALU_Subraction` computes `B = A - B`, not `B - A`. **This is intentional** (confirmed by the
  author) — `Sub A, B` means "B becomes A minus B", and `Rsub` is the variant that does `B - A`.
  Do not "fix" it; a test asserts it.
- Overflow is checked against the *unmasked* `uint` result, so a subtraction that borrows wraps to
  a huge `uint` and always sets `Overflow`. Asserted by test as current behavior — unlike the
  subtraction operand order this has not been confirmed as intended, so treat it as open.
