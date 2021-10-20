using System;
using System.IO;
using System.Reflection;

namespace C_
{
    class Emulator{
        //RAM & REGISTERS data structure
        public struct Data12Bit{
            public override string ToString() {
                return Val.ToString();
            }
            private uint _val;
            public uint Val {
                get => _val;
                set {_val = value&0xfff;}
            }
        }
        public Data12Bit[] RAM = new Data12Bit[4096];
        public Data12Bit[] REG = new Data12Bit[16];
        //Displays current values stored in RAM
        public void PrintRam(){
            Console.WriteLine("RAM Values:");
            for(int i = 0; i < RAM.Length; i += 16){
                Console.Write("0x" + i.ToString("X3") + ": ");
                for(int j = 0; j < 16; j++){
                    Console.Write(RAM[i+j].Val.ToString("X3") + " ");
                }
                Console.WriteLine();
            }
        }
        //Displays current values stored in Registers
        public void PrintReg(){
            Console.WriteLine("Register Values:");
            Console.Write("0x0:   ");
            for(int i = 0; i < 16; i++){
                    Console.Write(REG[i].Val.ToString("X3")+" ");
            }
            Console.WriteLine();
        }
        //Displays current flags stored in Register 14
        public void PrintFlags(){
            Console.WriteLine(GetFlags((Flags)15));
        }
        [Flags]
        public enum Flags{
            NONE = 0,
            A_GREATER = 1,
            B_GREATER = 2,
            EQUAL = 4,
            OVERFLOW = 8,
            ALL = 15
        };
        public Flags GetFlags(Flags check){
            return check&(Flags)REG[14].Val;
        }
        public void LoadProgram(string path){
            string program_path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);
            if (File.Exists(program_path)) {
                int ram_pointer = 0;
                string[] program_ram = File.ReadAllLines(program_path);
                foreach (string line in program_ram){
                    if (!line.StartsWith("//")){
                        uint value = UInt32.Parse(line, System.Globalization.NumberStyles.HexNumber);
                        RAM[ram_pointer].Val = value;
                        ram_pointer = ram_pointer + 1;
                    }
                }
            } else {
            Console.WriteLine("Program file does not exist");
            System.Environment.Exit(1);
            }
        }
        private bool isCPUrunning = true;
        public bool isRunning(){
            return isCPUrunning;
        }
        public void NextCommand(){
            uint flagbuffer = 0;
            var opcode = RAM[REG[15].Val].Val;
            var instruction = opcode&0xf;
            var arg_a = (opcode>>4)&0xf;
            var arg_b = (opcode>>8)&0xf;
            REG[15].Val++;
            Console.WriteLine("Next");
            switch (instruction){
                case 0:
                    switch (arg_a){
                        case 0:
                            switch (arg_b){
                                case 0:
                                    // Stop
                                    isCPUrunning = false;
                                    break;
                                case 1:
                                    // Conditional Stop
                                    if (GetFlags((Flags)REG[13].Val) != 0){
                                        isCPUrunning = false;
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
                            REG[15].Val++;
                            REG[arg_b].Val = RAM[(REG[15].Val - 1)].Val;
                            break;
                        case 2:
                            // Conditional Move N1, Reg[b]
                            if (GetFlags(Flags.ALL) != 0){
                                REG[15].Val++;
                                REG[arg_b].Val = RAM[(REG[15].Val - 1)].Val;
                            }
                            break;
                        case 3:
                            // Increment Reg[b]
                            flagbuffer = REG[arg_b].Val++;
                                if (flagbuffer > 4095){
                                    REG[14].Val = REG[14].Val|(uint)Flags.OVERFLOW;
                                }
                                REG[arg_b].Val = flagbuffer;
                            break;
                        case 4:
                            // Decrement Reg[b]
                            flagbuffer = REG[arg_b].Val--;
                                if (flagbuffer > 4095){
                                    REG[14].Val = REG[14].Val|(uint)Flags.OVERFLOW;
                                }
                                REG[arg_b].Val = flagbuffer;
                            break;
                        case 5:
                            // Not Reg[b] (done as xor with max value because you cannot negate uint values)
                            REG[arg_b].Val = REG[arg_b].Val^0xfff;
                            break;
                        case 6:
                            // Right Shift
                            flagbuffer = REG[arg_b].Val&0x1;
                                if (flagbuffer == 1){
                                    REG[14].Val = REG[14].Val|(uint)Flags.OVERFLOW;
                                }
                                REG[arg_b].Val = REG[arg_b].Val>>1;
                            Console.WriteLine("PLACEHOLDER");
                            break;
                        case 7:
                            // User Input Interrupt
                            while(true){
                                try{
                                    Console.WriteLine("Please Input Data (HEX) for the CPU: ");
                                    REG[arg_b].Val = UInt32.Parse(Console.ReadLine(), System.Globalization.NumberStyles.HexNumber);
                                    break;
                                }catch(Exception){
                                    Console.WriteLine("Invalid Value");
                                }
                            }
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
                    flagbuffer = REG[arg_a].Val + REG[arg_b].Val;
                    if (flagbuffer > 4095){
                        REG[14].Val = REG[14].Val|(uint)Flags.OVERFLOW;
                    }
                    REG[arg_b].Val = flagbuffer;
                    break;
                case 2:
                    flagbuffer = REG[arg_a].Val - REG[arg_b].Val;
                    if (flagbuffer > 4095){
                        REG[14].Val = REG[14].Val|(uint)Flags.OVERFLOW;
                    }
                    REG[arg_b].Val = flagbuffer;
                    break;
                case 3:
                    flagbuffer = REG[arg_b].Val - REG[arg_a].Val;
                    if (flagbuffer > 4095){
                        REG[14].Val = REG[14].Val|(uint)Flags.OVERFLOW;
                    }
                    REG[arg_b].Val = flagbuffer;
                    break;
                case 4:
                    REG[arg_b].Val = REG[arg_a].Val & REG[arg_b].Val;
                    break;
                case 5:
                    REG[arg_b].Val = REG[arg_a].Val | REG[arg_b].Val;
                    break;
                case 6:
                    REG[arg_b].Val = REG[arg_a].Val ^ REG[arg_b].Val;
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
                    if (REG[arg_b].Val > REG[arg_a].Val){
                        REG[14].Val = REG[14].Val|(uint)Flags.B_GREATER;
                    } else if (REG[arg_b].Val < REG[arg_a].Val){
                        REG[14].Val = REG[14].Val|(uint)Flags.A_GREATER;
                    } else if (REG[arg_b].Val == REG[arg_a].Val){
                        REG[14].Val = REG[14].Val|(uint)Flags.EQUAL;
                    } else {
                        Console.WriteLine("Comparation Error");
                        System.Environment.Exit(1);
                    }
                    break;
                case 12:
                    if (GetFlags((Flags)REG[13].Val) != 0){
                        REG[arg_b].Val = REG[arg_a].Val;
                    }
                    break;
                case 13:
                    REG[arg_b].Val = REG[arg_a].Val;
                    break;
                case 14:
                    RAM[REG[arg_b].Val].Val = REG[arg_a].Val;
                    break;
                case 15:
                    REG[arg_b].Val = RAM[REG[arg_a].Val].Val;
                    break;
                default:
                    Console.WriteLine("Program Error (Invalid Command)");
                    System.Environment.Exit(1);
                    break;
            }
        }
    }
    class Program {
        static void Main()
        {
            var emulator = new Emulator();
            emulator.LoadProgram(@"..\..\..\data\program.txt");
            emulator.PrintRam();
            while(emulator.isRunning()){
                emulator.NextCommand();
            }
            emulator.PrintRam();
            emulator.PrintReg();
        }
    }
}