using System;
using System.IO;
using System.Reflection;

namespace CpuEmulator
{
    class Emulator{
        //RAM & REGISTERS data structure
        public struct Data12Bit{
            public override string ToString() {
                return ValA.ToString();
            }
            private uint _val;
            public uint ValA {
                get => _val;
                set => _val = value&0xfff;
            }
            public uint ValB {
                get => _val;
                set => _val = value;
            }
        }
        public Data12Bit[] RAM = new Data12Bit[4096];
        public Data12Bit[] REG = new Data12Bit[16];
        public int total_insr = 0;
        public void LoadProgram(string path){
            string program_path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);
            if (File.Exists(program_path)) {
                int ram_pointer = 0;
                string[] program_ram = File.ReadAllLines(program_path);
                foreach (string line in program_ram){
                    if (!line.StartsWith("//")){
                        uint Value = UInt32.Parse(line, System.Globalization.NumberStyles.HexNumber);
                        RAM[ram_pointer].ValA = Value;
                        ram_pointer = ram_pointer + 1;
                    }
                }
            } else {
                Console.WriteLine("Program file does not exist");
                System.Environment.Exit(1);
            }
        }
        //Displays current Values stored in RAM
        public void PrintRam(){
            Console.WriteLine("RAM Values:");
            for(int i = 0; i < RAM.Length; i += 16){
                Console.Write("0x" + i.ToString("X3") + ": ");
                for(int j = 0; j < 16; j++){
                    Console.Write(RAM[i+j].ValA.ToString("X3") + " ");
                }
                Console.WriteLine();
            }
        }
        //Displays current Values stored in Registers
        public void PrintReg(){
            Console.WriteLine("Register Values:");
            Console.Write("0x0:   ");
            for(int i = 0; i < 16; i++){
                    Console.Write(REG[i].ValA.ToString("X3")+" ");
            }
            Console.WriteLine();
        }
        //Displays current flags stored in Register 14
        public void PrintFlags(){
            Console.WriteLine(GetFlags((Flags)15));
        }
        [Flags]
        public enum Flags{
            None = 0,
            AGreater = 1,
            BGreater = 2,
            Equal = 4,
            Overflow = 8,
            All = 15
        };

        private Flags GetFlags(Flags check){
            return check&(Flags)REG[14].ValA;
        }
        private bool _isCpuRunning = true;
        public bool IsRunning(){
            return _isCpuRunning;
        }
        public void NextCommand(){
            uint flagbuffer = 0;
            var opcode = RAM[REG[15].ValA].ValA;
            var instruction = opcode&0xf;
            var arg_a = (opcode>>4)&0xf;
            var arg_b = (opcode>>8)&0xf;
            REG[15].ValA++;
            switch (instruction){
                case 0:
                    switch (arg_a){
                        case 0:
                            switch (arg_b){
                                case 0:
                                    // Stop
                                    _isCpuRunning = false;
                                    break;
                                case 1:
                                    // Conditional Stop
                                    if (GetFlags((Flags)REG[13].ValA) != 0){
                                        _isCpuRunning = false;
                                    }
                                    break;
                                case 2:
                                    Console.WriteLine("PLACEHOLDER");
                                    break;
                                case 3:
                                    Console.WriteLine("PLACEHOLDER");
                                    break;
                                case 4:
                                    Console.WriteLine("PLACEHOLDER");
                                    break;
                                case 5:
                                    Console.WriteLine("PLACEHOLDER");
                                    break;
                                case 6:
                                    Console.WriteLine("PLACEHOLDER");
                                    break;
                                case 7:
                                    Console.WriteLine("PLACEHOLDER");
                                    break;
                                case 8:
                                    Console.WriteLine("PLACEHOLDER");
                                    break;
                                case 9:
                                    Console.WriteLine("PLACEHOLDER");
                                    break;
                                case 10:
                                    Console.WriteLine("PLACEHOLDER");
                                    break;
                                case 11:
                                    Console.WriteLine("PLACEHOLDER");
                                    break;
                                case 12:
                                    Console.WriteLine("PLACEHOLDER");
                                    break;
                                case 13:
                                    Console.WriteLine("PLACEHOLDER");
                                    break;
                                case 14:
                                    Console.WriteLine("PLACEHOLDER");
                                    break;
                                case 15:
                                    Console.WriteLine("PLACEHOLDER");
                                    break;
                                default:
                                    Console.WriteLine("Program Error (Invalid Command)");
                                    System.Environment.Exit(1);
                                    break;
                            }
                            break;
                        case 1:
                            // Move N1, Reg[b]
                            REG[15].ValA++;
                            REG[arg_b].ValB = RAM[(REG[15].ValA - 1)].ValA;
                            break;
                        case 2:
                            // Conditional Move N1, Reg[b]
                            if (GetFlags(Flags.All) != 0){
                                REG[15].ValA++;
                                REG[arg_b].ValB = RAM[(REG[15].ValA - 1)].ValA;
                            }
                            break;
                        case 3:
                            // Increment Reg[b]
                            flagbuffer = REG[arg_b].ValA + 1;
                                if (flagbuffer > 4095){
                                    REG[14].ValB = REG[14].ValA|(uint)Flags.Overflow;
                                    REG[arg_b].ValA = flagbuffer;
                                } else {
                                    REG[arg_b].ValB = flagbuffer;
                                }
                            break;
                        case 4:
                            // Decrement Reg[b]
                            flagbuffer = REG[arg_b].ValA - 1;
                                if (flagbuffer > 4095){
                                    REG[14].ValB = REG[14].ValA|(uint)Flags.Overflow;
                                    REG[arg_b].ValA = flagbuffer;
                                } else {
                                    REG[arg_b].ValB = flagbuffer;
                                }
                            break;
                        case 5:
                            // Not Reg[b] (done as xor with max Value because you cannot negate uint Values)
                            REG[arg_b].ValB = REG[arg_b].ValA^0xfff;
                            break;
                        case 6:
                            // Right Shift
                            flagbuffer = REG[arg_b].ValA&0x1;
                                if (flagbuffer == 1){
                                    REG[14].ValB = REG[14].ValA|(uint)Flags.Overflow;
                                }
                                REG[arg_b].ValB = REG[arg_b].ValA>>1;
                            Console.WriteLine("PLACEHOLDER");
                            break;
                        case 7:
                            // User Input Interrupt
                            while(true){
                                try{
                                    Console.WriteLine("Please Input Data (HEX) for the CPU: ");
                                    REG[arg_b].ValA = UInt32.Parse(Console.ReadLine() ?? throw new Exception(), System.Globalization.NumberStyles.HexNumber);
                                    break;
                                }catch(Exception){
                                    Console.WriteLine("Invalid Value");
                                }
                            }
                            break;
                        case 8:
                            // Addition N1 + Reg
                            REG[15].ValA++;
                            flagbuffer = RAM[(REG[15].ValA - 1)].ValA + REG[arg_b].ValA;
                            if (flagbuffer > 4095){
                                REG[14].ValB = REG[14].ValA|(uint)Flags.Overflow;
                                REG[arg_b].ValA = flagbuffer;
                            } else {
                                REG[arg_b].ValB = flagbuffer;
                            }
                            break;
                        case 9:
                            // Subtract N1 - Reg
                            REG[15].ValA++;
                            flagbuffer = RAM[(REG[15].ValA - 1)].ValA - REG[arg_b].ValA;
                            if (flagbuffer > 4095){
                                REG[14].ValB = REG[14].ValA|(uint)Flags.Overflow;
                                REG[arg_b].ValA = flagbuffer;
                            } else {
                                REG[arg_b].ValB = flagbuffer;
                            }
                            break;
                        case 10:
                            // Reversed Subtract Reg - N1
                            REG[15].ValA++;
                            flagbuffer = REG[arg_b].ValA - RAM[(REG[15].ValA - 1)].ValA;
                            if (flagbuffer > 4095){
                                REG[14].ValB = REG[14].ValA|(uint)Flags.Overflow;
                                REG[arg_b].ValA = flagbuffer;
                            } else {
                                REG[arg_b].ValB = flagbuffer;
                            }
                            break;
                        case 11:
                            // AND N1 & Reg
                            REG[15].ValA++;
                            REG[arg_b].ValB = RAM[(REG[15].ValA - 1)].ValA & REG[arg_b].ValA;
                            break;
                        case 12:
                            // OR N1 | Reg
                            REG[15].ValA++;
                            REG[arg_b].ValB = RAM[(REG[15].ValA - 1)].ValA | REG[arg_b].ValA;
                            break;
                        case 13:
                            // XOR N1 ^ Reg
                            REG[15].ValA++;
                            REG[arg_b].ValB = RAM[(REG[15].ValA - 1)].ValA ^ REG[arg_b].ValA;
                            break;
                        case 14:
                            // Number Comparasion N1 ? Reg
                            REG[15].ValA++;
                            if (REG[arg_b].ValA > RAM[(REG[15].ValA - 1)].ValA){
                                REG[14].ValB = REG[14].ValA|(uint)Flags.BGreater;
                            } else if (REG[arg_b].ValA < RAM[(REG[15].ValA - 1)].ValA){
                                REG[14].ValB = REG[14].ValA|(uint)Flags.AGreater;
                            } else if (REG[arg_b].ValA == RAM[(REG[15].ValA - 1)].ValA){
                                REG[14].ValB = REG[14].ValA|(uint)Flags.Equal;
                            } else {
                                Console.WriteLine("Comparation Error");
                                Environment.Exit(1);
                            }
                            break;
                        case 15:
                            Console.WriteLine("PLACEHOLDER");
                            break;
                        default:
                            Console.WriteLine("Program Error (Invalid Command)");
                            Environment.Exit(1);
                            break;
                    }
                    break;
                case 1:
                    // Addition
                    flagbuffer = REG[arg_a].ValA + REG[arg_b].ValA;
                    if (flagbuffer > 4095){
                        REG[14].ValB = REG[14].ValA|(uint)Flags.Overflow;
                        REG[arg_b].ValA = flagbuffer;
                    } else {
                        REG[arg_b].ValB = flagbuffer;
                    }
                    break;
                case 2:
                    // Subtract
                    flagbuffer = REG[arg_a].ValA - REG[arg_b].ValA;
                    if (flagbuffer > 4095){
                        REG[14].ValB = REG[14].ValA|(uint)Flags.Overflow;
                        REG[arg_b].ValA = flagbuffer;
                    } else {
                        REG[arg_b].ValB = flagbuffer;
                    }
                    break;
                case 3:
                    // Reversed Subtract
                    flagbuffer = REG[arg_b].ValA - REG[arg_a].ValA;
                    if (flagbuffer > 4095){
                        REG[14].ValB = REG[14].ValA|(uint)Flags.Overflow;
                        REG[arg_b].ValA = flagbuffer;
                    } else {
                        REG[arg_b].ValB = flagbuffer;
                    }
                    break;
                case 4:
                    // AND
                    REG[arg_b].ValB = REG[arg_a].ValA & REG[arg_b].ValA;
                    break;
                case 5:
                    // OR
                    REG[arg_b].ValB = REG[arg_a].ValA | REG[arg_b].ValA;
                    break;
                case 6:
                    // XOR
                    REG[arg_b].ValB = REG[arg_a].ValA ^ REG[arg_b].ValA;
                    break;
                case 7:
                    Console.WriteLine("PLACEHOLDER");
                    break;
                case 8:
                    Console.WriteLine("PLACEHOLDER");
                    break;
                case 9:
                    Console.WriteLine("PLACEHOLDER");
                    break;
                case 10:
                    Console.WriteLine("PLACEHOLDER");
                    break;
                case 11:
                    // Number Comparasion
                    if (REG[arg_b].ValA > REG[arg_a].ValA){
                        REG[14].ValB = REG[14].ValA|(uint)Flags.BGreater;
                    } else if (REG[arg_b].ValA < REG[arg_a].ValA){
                        REG[14].ValB = REG[14].ValA|(uint)Flags.AGreater;
                    } else if (REG[arg_b].ValA == REG[arg_a].ValA){
                        REG[14].ValB = REG[14].ValA|(uint)Flags.Equal;
                    } else {
                        Console.WriteLine("Comparation Error");
                        Environment.Exit(1);
                    }
                    break;
                case 12:
                    // Conditional Move Reg -> Reg
                    if (GetFlags((Flags)REG[13].ValA) != 0){
                        REG[arg_b].ValB = REG[arg_a].ValA;
                    }
                    break;
                case 13:
                    // Move Reg -> Reg
                    REG[arg_b].ValB = REG[arg_a].ValA;
                    break;
                case 14:
                    // Move Reg -> RAM
                    RAM[REG[arg_b].ValA].ValB = REG[arg_a].ValA;
                    break;
                case 15:
                    // Move RAM -> Reg
                    REG[arg_b].ValB = RAM[REG[arg_a].ValA].ValA;
                    break;
                default:
                    Console.WriteLine("Program Error (Invalid Command)");
                    Environment.Exit(1);
                    break;
            }
        }
    }

    static class Program {
        static void Main(){
            var emulator = new Emulator();
            emulator.LoadProgram(@"..\..\..\data\program.txt");
            emulator.PrintRam();
            while(emulator.IsRunning()){
                emulator.NextCommand();
            }
            emulator.PrintRam();
            emulator.PrintReg();
        }
    }
}