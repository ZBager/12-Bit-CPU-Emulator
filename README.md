# 12-Bit-CPU-Emulator

An emulator for a custom 12-bit CPU — 4096 words of RAM, 16 registers — with a cross-platform
desktop UI built on Avalonia and .NET 10.

## Running it

```
dotnet run --project src/CpuEmulator.App                    # launch the UI
dotnet run --project src/CpuEmulator.App data/program.txt   # launch with a program preloaded
dotnet test                                                 # run the test suite
```

Prebuilt downloads are produced by CI for Linux (AppImage), macOS (Intel and Apple Silicon) and
Windows. The Linux AppImage needs `chmod +x` after download.

**The macOS builds are unsigned.** macOS quarantines unsigned apps downloaded from the internet
and reports them as *"damaged and can't be opened"*, which is misleading — the download is fine.
To run it:

```
xattr -dr com.apple.quarantine "CPU Emulator.app"
```

## Instruction set

Registers 13, 14 and 15 are architectural: **15** is the program counter (so a jump is just a write
to it), **14** holds the flags, and **13** is the condition mask. Conditional instructions run when
`REG[13] & REG[14]` is non-zero. Flags are only ever set by the CPU — a program clears them by
writing 0 to `REG[14]` itself.

Operand order reads as *"instruction source, destination"*, and the destination is always `Reg[b]`.
So `Sub Reg[a], Reg[b]` computes `Reg[b] = Reg[a] - Reg[b]`, and `Rsub` is the variant that
computes `Reg[b] - Reg[a]`. Instructions taking an immediate (`N1`) consume the **following word**
in RAM, so they occupy two words.


| Arg B            | Arg A            | &nbsp; &nbsp; &nbsp; Op Code &nbsp; &nbsp; &nbsp; |Description |
| :--------------: | :--------------: | :--------------: | -------------------------------- |
| Used as Address  | Used as Address  | 1                | Add Reg[a], Reg[b]               |
| Used as Address  | Used as Address  | 2                | Sub Reg[a], Reg[b]               |
| Used as Address  | Used as Address  | 3                | Rsub Reg[a], Reg[b]              |
| Used as Address  | Used as Address  | 4                | And Reg[a], Reg[b]               |
| Used as Address  | Used as Address  | 5                | Or Reg[a], Reg[b]                |
| Used as Address  | Used as Address  | 6                | Xor Reg[a], Reg[b]               |
| Used as Address  | Used as Address  | 7                | Empty Instruction                |
| Used as Address  | Used as Address  | 8                | Empty Instruction                |
| Used as Address  | Used as Address  | 9                | Empty Instruction                |
| Used as Address  | Used as Address  | A                | Empty Instruction                |
| Used as Address  | Used as Address  | B                | Compare Reg[a], Reg[b]           |
| Used as Address  | Used as Address  | C                | Conditional Move Reg[a], Reg[b]  |
| Used as Address  | Used as Address  | D                | Mov Reg[a], Reg[b]               |
| Used as Address  | Used as Address  | E                | Mov Ram[Reg[a]], Reg[b]          |
| Used as Address  | Used as Address  | F                | Mov Reg[b], Ram[Reg[a]]          |
| **Arg B**        | **Op Code**      | **Op Code**      |                                  |
| Used as Address  | 1                | 0                | Mov N1, Reg[b]                   |
| Used as Address  | 2                | 0                | Conditional Mov N1, Reg[b]       |
| Used as Address  | 3                | 0                | Inc Reg[b]                       |
| Used as Address  | 4                | 0                | Dec Reg[b]                       |
| Used as Address  | 5                | 0                | Not Reg[b]                       |
| Used as Address  | 6                | 0                | Rsh Reg[b]                       |
| Used as Address  | 7                | 0                | User Input, Reg[b]               |
| Used as Address  | 8                | 0                | Add N1, Reg[b]                   |
| Used as Address  | 9                | 0                | Sub N1, Reg[b]                   |
| Used as Address  | A                | 0                | Rsub N1, Reg[b]                  |
| Used as Address  | B                | 0                | And N1, Reg[b]                   |
| Used as Address  | C                | 0                | Or N1, Reg[b]                    |
| Used as Address  | D                | 0                | Xor N1, Reg[b]                   |
| Used as Address  | E                | 0                | Compare N1, Reg[b]               |
| Used as Address  | F                | 0                | Empty Instruction                |
| **Op Code**      | **Op Code**      | **Op Code**      |                                  |
| 0                | 0                | 0                | Stop                             |
| 1                | 0                | 0                | Conditional Stop                 |
| 2                | 0                | 0                | *(not implemented)*              |
| 3                | 0                | 0                | Empty Instruction                |
| 4                | 0                | 0                | Empty Instruction                |
| 5                | 0                | 0                | Empty Instruction                |
| 6                | 0                | 0                | Empty Instruction                |
| 7                | 0                | 0                | Empty Instruction                |
| 8                | 0                | 0                | Empty Instruction                |
| 9                | 0                | 0                | Empty Instruction                |
| A                | 0                | 0                | Empty Instruction                |
| B                | 0                | 0                | Empty Instruction                |
| C                | 0                | 0                | Empty Instruction                |
| D                | 0                | 0                | Empty Instruction                |
| E                | 0                | 0                | Empty Instruction                |
| F                | 0                | 0                | Empty Instruction                |
