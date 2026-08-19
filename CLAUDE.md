# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

An emulator for a custom 12-bit CPU (4096 words of RAM, 16 registers), plus a GUI that shows
RAM/register contents live while a program runs. `README.md` holds the authoritative-ish
instruction-set table; see "README vs. implementation" below for known drift.

**This repo is mid-migration** from WPF on .NET Framework 4.7.2 to Avalonia 12 on .NET 10.
Phase 1 (SDK-style projects, core extracted) is done and the WPF app has been deleted. The
emulator core lives in `src/CpuEmulator.Core` and builds and runs on Linux. **There is no UI
project right now** — the Avalonia app arrives in Phase 4. The old WPF sources are recoverable
from git history if the port needs them:

```
git show main:MainWindow.xaml      # and App.xaml, MainWindow.xaml.cs, App.xaml.cs
```

## Build & run

.NET 10, SDK-style projects, cross-platform. Builds on Linux:

```
dotnet build          # solution: CpuEmulator.sln (classic .sln, not .slnx, for VS compat)
```

`src/CpuEmulator.Core` is a class library with no UI dependencies and no entry point, so there is
nothing to `dotnet run` yet — the Avalonia app arrives in Phase 4. To exercise the CPU today, add a
throwaway console project referencing the Core project.

There are still no tests (they arrive in Phase 3) and no linter.
`.github/workflows/dotnet-desktop.yml` is unmodified GitHub boilerplate — its
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

## Program files

`data/*.txt`: one 3-hex-digit word per line, `//`-prefixed lines are comments (existing comments are
Polish; `program.txt` is CP1250 with CRLF endings, `program1.txt` is UTF-8). `LoadProgram` still
resolves paths relative to the *assembly location*, which is broken on Linux — `Path.Combine` does
not normalize the `..\..\data\` backslashes and yields one literal filename. Phase 2 replaces this
with a pure parse function plus a thin file wrapper, and Phase 5 marks the data files as
`CopyToOutputDirectory`. Passing an absolute path works today as a stopgap.

Loading does not clear RAM first, and there is no assembler — programs are hand-assembled hex.
`program.txt` is a 63-word bubble sort with no user-input opcode, so it runs unattended to a halt
(1655 instructions, data at `0x30..0x3F` sorted ascending) — this is the Phase 3 golden test.
`program1.txt` uses opcode `270` (user input into `REG[2]`) and needs a scripted input source.

## README vs. implementation

The README table has drifted from `Emulator.cs`; trust the code and fix the README when you touch it:

- L0 opcodes `E` and `F` are swapped relative to the table — in code, `14` is
  `Mov RAM[REG[a]], REG[b]` (store) and `15` is `Mov REG[b], RAM[REG[a]]` (load).
- "Clear Flags" (`2,0,0`) is documented but not implemented in `ExecuteCommand_L2`.
- `ALU_Subraction` computes `B = A - B`, not `B - A` (the reversed variant is the one that does
  `B - A`). Overflow is checked against the *unmasked* `uint` result, so subtraction that borrows
  wraps to a huge `uint` and always sets `Overflow`.
