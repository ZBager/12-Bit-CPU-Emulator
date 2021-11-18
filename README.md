# 12-Bit-CPU-Emulator

| Arg B  | Arg A | Op Code | Description |
| ------------- | ------------- | ------------- | ------------- |
| Used as Address  | Used as Address  | 1  | Add Reg[a], Reg[b]  |
| Used as Address  | Used as Address  | 2  | Sub Reg[a], Reg[b]  |
| Used as Address  | Used as Address  | 3  | Rsub Reg[a], Reg[b]  |
| Used as Address  | Used as Address  | 4  | And Reg[a], Reg[b]  |
| Used as Address  | Used as Address  | 5  | Or Reg[a], Reg[b]  |
| Used as Address  | Used as Address  | 6  | Xor Reg[a], Reg[b]  |
| Used as Address  | Used as Address  | 7  | Empty Instruction  |
| Used as Address  | Used as Address  | 8  | Empty Instruction  |
| Used as Address  | Used as Address  | 9  | Empty Instruction  |
| Used as Address  | Used as Address  | A  | Empty Instruction  |
| Used as Address  | Used as Address  | B  | Compare Reg[a], Reg[b]  |
| Used as Address  | Used as Address  | C  | Conditional Move Reg[a], Reg[b]  |
| Used as Address  | Used as Address  | D  | Mov Reg[a], Reg[b]  |
| Used as Address  | Used as Address  | E  | Mov Reg[a], Ram[Reg[b]]  |
| Used as Address  | Used as Address  | F  | Mov Ram[Reg[a]], Reg[b]  |
| **Arg B** | **Op Code** | **Op Code** | **-------------** |
| Used as Address  | 1  | 0  | Mov N1, Reg[b]  |
| Used as Address  | 2  | 0  | Conditional Mov N1, Reg[b]  |
| Used as Address  | 3  | 0  | Inc Reg[b]  |
| Used as Address  | 4  | 0  | Dec Reg[b]  |
| Used as Address  | 5  | 0  | Not Reg[b]  |
| Used as Address  | 6  | 0  | Rsh Reg[b]  |
| Used as Address  | 7  | 0  | User Input, Reg[b]  |
| Used as Address  | 8  | 0  | Empty Instruction  |
| Used as Address  | 9  | 0  | Empty Instruction  |
| Used as Address  | A  | 0  | Empty Instruction  |
| Used as Address  | B  | 0  | Empty Instruction  |
| Used as Address  | C  | 0  | Empty Instruction  |
| Used as Address  | D  | 0  | Empty Instruction  |
| Used as Address  | E  | 0  | Empty Instruction  |
| Used as Address  | F  | 0  | Empty Instruction  |
| **Op Code** | **Op Code** | **Op Code** | **-------------** |
| 0  | 0  | 0  | Stop  |
| 1  | 0  | 0  | Conditional Stop  |
| 2  | 0  | 0  | Clear Flags  |
| 3  | 0  | 0  | Empty Instruction  |
| 4  | 0  | 0  | Empty Instruction  |
| 5  | 0  | 0  | Empty Instruction  |
| 6  | 0  | 0  | Empty Instruction  |
| 7  | 0  | 0  | Empty Instruction  |
| 8  | 0  | 0  | Empty Instruction  |
| 9  | 0  | 0  | Empty Instruction  |
| A  | 0  | 0  | Empty Instruction  |
| B  | 0  | 0  | Empty Instruction  |
| C  | 0  | 0  | Empty Instruction  |
| D  | 0  | 0  | Empty Instruction  |
| E  | 0  | 0  | Empty Instruction  |
| F  | 0  | 0  | Empty Instruction  |
