﻿using System;
using System.IO;
using System.Reflection;

namespace C_
{
    class Emulator {
        //Struktura danych dla Ramu oraz Rejestrów
        public struct Data12Bit{
            public override string ToString() {
                return Val.ToString();
            }
            private uint _reg;
            public uint Val {
                get => _reg;
                set {_reg = value&0xfff;}
            }
            public Data12Bit(uint aaa) {
                _reg = aaa&0xfff;
            }
        }
        public Data12Bit[] RAM = new Data12Bit[4096];
        public Data12Bit[] REG = new Data12Bit[16];
        //Wyświetlanie zawartości Ramu
        public void PrintRam(){
            Console.WriteLine("Zawartosc Ramu:");
            for(int i = 0; i < RAM.Length; i += 16){
                for(int j = 0; j < 16; j++){
                    Console.Write(RAM[i+j].Val.ToString("X3")+" ");
                }
                Console.WriteLine();
            }
        }
        public void PrintReg(){
            Console.WriteLine("Zawartosc Rejestrow:");
            for(int i = 0; i < 16; i++){
                    Console.Write(REG[i].Val.ToString("X3")+" ");
            }
            Console.WriteLine();
        }
        [Flags]
        public enum Flags{
            A_GREATER = 1,
            B_GREATER = 2,
            EQUAL = 4,
            OVERFLOW = 8
        };
        public Flags GetFlags(Flags check){
            return check;
        }
        public void LoadProgram(string path){
            string program_path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);
            if (File.Exists(program_path)) {
                int ram_pointer = 0;
                string[] program_ram = File.ReadAllLines(program_path);
                foreach (string line in program_ram) {
                uint value = UInt32.Parse(line, System.Globalization.NumberStyles.HexNumber);
                RAM[ram_pointer].Val = value;
                ram_pointer = ram_pointer + 1;
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
        int programcounter = 0;
        public void NextCommand(){
            var opcode = RAM[programcounter].Val;
            var instruction = opcode&0xf;
            var arg_a = (opcode>>4)&0xf;
            var arg_b = (opcode>>8)&0xf;
            switch (instruction){
                case 0:
                    switch (arg_a){
                        case 0:
                            switch (arg_b){
                                case 0:
                                    isCPUrunning = false;
                                    break;
                                case 1:
                                    Console.WriteLine("dziala");
                                    break;
                                case 2:
                                    Console.WriteLine("dziala");
                                    break;
                                case 3:
                                    Console.WriteLine("dziala");
                                    break;
                                case 4:
                                    Console.WriteLine("dziala");
                                    break;
                                case 5:
                                    Console.WriteLine("dziala");
                                    break;
                                case 6:
                                    Console.WriteLine("dziala");
                                    break;
                                case 7:
                                    Console.WriteLine("dziala");
                                    break;
                                case 8:
                                    Console.WriteLine("dziala");
                                    break;
                                case 9:
                                    Console.WriteLine("dziala");
                                    break;
                                case 10:
                                    Console.WriteLine("dziala");
                                    break;
                                case 11:
                                    Console.WriteLine("dziala");
                                    break;
                                case 12:
                                    Console.WriteLine("dziala");
                                    break;
                                case 13:
                                    Console.WriteLine("dziala");
                                    break;
                                case 14:
                                    Console.WriteLine("dziala");
                                    break;
                                case 15:
                                    Console.WriteLine("dziala");
                                    break;
                                default:
                                    Console.WriteLine("Program Error (Invalid Command)");
                                    System.Environment.Exit(1);
                                    break;
                            }
                            break;
                        case 1:
                            programcounter++;
                            REG[arg_b].Val = RAM[programcounter].Val;
                            break;
                        case 2:
                            Console.WriteLine("dziala");
                            break;
                        case 3:
                            Console.WriteLine("dziala");
                            break;
                        case 4:
                            Console.WriteLine("dziala");
                            break;
                        case 5:
                            Console.WriteLine("dziala");
                            break;
                        case 6:
                            Console.WriteLine("dziala");
                            break;
                        case 7:
                            Console.WriteLine("dziala");
                            break;
                        case 8:
                            Console.WriteLine("dziala");
                            break;
                        case 9:
                            Console.WriteLine("dziala");
                            break;
                        case 10:
                            Console.WriteLine("dziala");
                            break;
                        case 11:
                            Console.WriteLine("dziala");
                            break;
                        case 12:
                            Console.WriteLine("dziala");
                            break;
                        case 13:
                            Console.WriteLine("dziala");
                            break;
                        case 14:
                            Console.WriteLine("dziala");
                            break;
                        case 15:
                            Console.WriteLine("dziala");
                            break;
                        default:
                            Console.WriteLine("Program Error (Invalid Command)");
                            System.Environment.Exit(1);
                            break;
                    }
                    break;
                case 1:
                    REG[arg_b].Val = REG[arg_a].Val + REG[arg_b].Val;
                    break;
                case 2:
                    REG[arg_b].Val = REG[arg_a].Val - REG[arg_b].Val;
                    break;
                case 3:
                    REG[arg_b].Val = REG[arg_b].Val - REG[arg_a].Val;
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
                    Console.WriteLine("dziala");
                    break;
                case 8:
                    Console.WriteLine("dziala");
                    break;
                case 9:
                    Console.WriteLine("dziala");
                    break;
                case 10:
                    Console.WriteLine("dziala");
                    break;
                case 11:
                    Console.WriteLine("dziala");
                    break;
                case 12:
                    Console.WriteLine("dziala");
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
            programcounter++;
        }
    }
    class Program {
        static void Main()
        {
            var example = Emulator.Flags.A_GREATER|Emulator.Flags.OVERFLOW;
            var emulator = new Emulator();
            emulator.LoadProgram(@"..\..\..\data\program.txt");
            emulator.PrintRam();
            while(emulator.isRunning()){
                emulator.NextCommand();
            }
            emulator.PrintRam();
            emulator.PrintReg();
            Console.WriteLine(example);
            
        }
    }
}