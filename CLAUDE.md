# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

An emulator for a custom 12-bit CPU (4096 words of RAM, 16 registers), plus a GUI that shows
RAM/register contents live while a program runs. `README.md` holds the authoritative-ish
instruction-set table; see "README vs. implementation" below for known drift.

**This repo is mid-migration** from WPF on .NET Framework 4.7.2 to Avalonia 12 on .NET 10.
**The migration is complete.** The emulator core lives in `src/CpuEmulator.Core`, the Avalonia 12
UI in `src/CpuEmulator.App`, and 57 tests in `tests/CpuEmulator.Tests`. One codebase builds and
runs on Linux, macOS and Windows. The only deferred item is the program editor described under
"The Avalonia UI" — a feature that never existed, not migration debt. The old WPF sources are recoverable
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

dotnet run --project src/CpuEmulator.App                    # launch the UI
dotnet run --project src/CpuEmulator.App data/program.txt   # launch with a program preloaded
```

The optional command-line path skips the file picker, which makes the app scriptable and is how
it gets driven when verifying a change by hand.

`src/CpuEmulator.Core` is a class library with no UI dependencies; `src/CpuEmulator.App` is the
Avalonia front end and the only project with an entry point.

There is no linter. `.github/workflows/build.yml` builds and tests on Linux, macOS and Windows,
then packages all three.

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

### The Avalonia UI

`src/CpuEmulator.App`, Avalonia 12, hand-rolled `INotifyPropertyChanged` (no MVVM framework).
Code-behind rather than view models, matching the shape of the WPF original.

- **`MemoryRow`** backs both grids. The WPF version rebuilt all 4,112 row objects every 10 ms
  because `Data12Bit` is a value type and copies do not track later writes. Rows now mutate in
  place and raise `PropertyChanged` only when a value actually moved, so an idle CPU costs
  nothing to display and Avalonia's virtualization is not fighting a fresh collection every tick.
- **Running** happens on a background `Task` with a `CancellationTokenSource`. `Thread.Abort()`
  does not exist on .NET 10, so Stop is cooperative: the token breaks the run loop *and* closes
  the input dialog, which is the only way to unblock a CPU parked on input.
- **Single-stepping also runs off the UI thread.** This looks unnecessary but is not: the
  user-input instruction blocks until a dialog is answered, and that dialog needs the UI thread.
  Stepping on the UI thread would deadlock the moment a program hit opcode 7.
- **`UiInputSource`** implements `IInputSource` by marshalling to the UI thread and blocking its
  caller. It must never be called *from* the UI thread — see above.
- **Errors surface in the status bar.** The core reports invalid opcodes as exceptions now, so
  there has to be somewhere for them to go; previously they killed the process silently.
- **Reset** reloads the last-loaded program, so it means "start this program over".

Two XAML notes for anyone porting more WPF markup: Avalonia 12 defaults to compiled bindings, so
every bound control needs an `x:DataType` or the build fails with `AVLN2100`; and
`DataGrid.AlternatingRowBackground` has no Avalonia equivalent — the striping is a
`DataGridRow:nth-child(2n)` selector in `App.axaml`.

The `programerData` grid and its Up/Down/Write buttons are still the program editor that was never
built. They are explicitly disabled and labelled rather than left looking usable.

## Packaging

`packaging/` holds everything CI uses, and each script runs locally too:

```
./packaging/linux/build-appimage.sh [out]        # 41 MB AppImage, x86_64
./packaging/macos/build-app.sh osx-arm64 [out]   # unsigned .app bundle
./packaging/macos/build-app.sh osx-x64   [out]
```

### Icons

`packaging/assets/cpuemulator.svg` is the **master**; every other icon is generated from it, so
edit the SVG and regenerate rather than touching the rasters:

```
rsvg-convert -w 1024 -h 1024 packaging/assets/cpuemulator.svg -o packaging/assets/cpuemulator.png
# .ico: render 16/32/48/64/128/256 and combine with `magick a.png b.png ... out.ico`
cp packaging/assets/cpuemulator.ico src/CpuEmulator.App/Assets/cpuemulator.ico
```

Where each one is consumed:

| File | Used by |
| --- | --- |
| `cpuemulator.svg` | master source |
| `cpuemulator.png` (1024px) | AppImage `.DirIcon`, and the macOS `.icns` built in CI |
| `cpuemulator.ico` (6 sizes) | `<ApplicationIcon>` on the Windows exe, and the Avalonia `Window.Icon` |
| `src/CpuEmulator.App/Assets/cpuemulator.ico` | embedded as an `AvaloniaResource`; the copy the running app loads |

The app copy is deliberate duplication — `AvaloniaResource` paths are resolved relative to the
project, so the app cannot reference `packaging/` without a link that breaks `dotnet publish`.
Keep the two in sync when the icon changes.

Notes worth knowing before touching these:

- **The macOS bundles cross-build from Linux** — a `.app` is only a directory layout plus an
  `Info.plist`. The icon step is skipped when `iconutil`/`sips` are missing, so a Linux build
  produces a valid but icon-less bundle. CI builds on a macOS runner, which is also the only
  place the result gets exercised.
- **The bundles are unsigned.** macOS quarantines them and reports "damaged", which is misleading;
  the README documents the `xattr -dr com.apple.quarantine` workaround. Signing needs a paid Apple
  Developer account and was a deliberate no for now.
- **`upload-artifact` zips its input and drops the executable bit**, which would leave a `.app`
  unlaunchable — so the workflow tars the bundle first. Don't "simplify" that away.
- `appimagetool` is not packaged by most distributions; the script fetches it on demand.
- Self-contained output is ~102 MB on Linux and ~105–111 MB on macOS. The AppImage compresses to
  about 41 MB. Trimming would cut this further but Avalonia leans on reflection for XAML, so it
  needs real testing before being turned on.

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

The README table used to contradict `Emulator.cs`; it was corrected once the golden tests pinned
the real behavior. Two things are worth keeping in mind if you edit either:

- `ALU_Subraction` computes `B = A - B`, not `B - A`. **This is intentional** (confirmed by the
  author) — `Sub A, B` means "B becomes A minus B", and `Rsub` is the variant that does `B - A`.
  Do not "fix" it; a test asserts it.
- Overflow is checked against the *unmasked* `uint` result, so a subtraction that borrows wraps to
  a huge `uint` and always sets `Overflow`. Asserted by test as current behavior — unlike the
  subtraction operand order this has never been confirmed as intended, so treat it as open.
