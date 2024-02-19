using System;
using System.IO;
using System.Reflection;

namespace CpuEmulator
{
	public class Emulator
	{
		//RAM & REGISTERS data structure
		public Data12Bit[] RAM = new Data12Bit[4096];
		public Data12Bit[] REG = new Data12Bit[16];

		//Constant Registers
		private uint CounterReg
		{
			get => REG[15].Val;
			set => REG[15].Val = value;
		}
		private uint FlagReg
		{
			get => REG[14].Val;
			set => REG[14].Val = value;
		}
		private uint CheckFlagReg
		{
			get => REG[13].Val;
			set => REG[13].Val = value;
		}
		//Try to find a file and if it exists load it to ram
		public void LoadProgram(string path)
		{
			string program_path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);

			if (File.Exists(program_path))
				LoadProgramToRam(File.ReadAllLines(program_path));
			else
				Console.WriteLine("File does not exist");
		}
		//Load selected file to ram
		private void LoadProgramToRam(string[] program)
		{
			int ram_pointer = 0;
			foreach (string line in program)
			{
				if (!line.StartsWith("//"))
				{
					uint Value = UInt32.Parse(line, System.Globalization.NumberStyles.HexNumber);
					RAM[ram_pointer].Val = Value;
					ram_pointer++;
				}
			}
		}

		//Displays Values stored in RAM
		public void PrintRam()
		{
			Console.WriteLine("RAM Values:");
			for (int i = 0; i < RAM.Length; i += 16)
			{
				Console.Write("0x" + i.ToString("X3") + ": ");
				for (int j = 0; j < 16; j++)
				{
					Console.Write(RAM[i + j].Val.ToString("X3") + " ");
				}
				Console.WriteLine();
			}
		}

		//Displays Values stored in Registers
		public void PrintReg()
		{
			Console.WriteLine("Register Values:");
			Console.Write("0x0:   ");
			for (int i = 0; i < 16; i++)
			{
				Console.Write(REG[i].Val.ToString("X3") + " ");
			}
			Console.WriteLine();
		}

		//Displays flags stored in Register 14
		public void PrintFlags()
		{
			Console.WriteLine(GetFlags((Flags)15));
		}

		// CPU flags. More flags can be added later.
		[Flags]
		public enum Flags
		{
			None        = 0b_0000_0000_0000,
			AGreater    = 0b_0000_0000_0001,
			BGreater    = 0b_0000_0000_0010,
			Equal       = 0b_0000_0000_0100,
			Overflow    = 0b_0000_0000_1000,
			All         = 0b_1111_1111_1111
		};

		private Flags GetFlags(Flags check)
		{
			return check & (Flags)FlagReg;
		}

		private bool _isCpuRunning = true;
		public bool IsRunning()
		{
			return _isCpuRunning;
		}
		public void NextCommand()
		{
			uint flagbuffer = 0;
			uint opcode = RAM[CounterReg].Val;
			uint instruction = opcode & 0xf;
			uint arg_a = (opcode >> 4) & 0xf;
			uint arg_b = (opcode >> 8) & 0xf;
			CounterReg++;
			ExecuteCommand_L0(instruction, arg_a, arg_b, flagbuffer);
		}

		private void ExecuteCommand_L0(uint instruction, uint arg_a, uint arg_b, uint flagbuffer)
		{
			switch (instruction)
			{
				case 0:
					ExecuteCommand_L1(arg_a, arg_b, flagbuffer);
					break;
				case 1:
					// Addition
					flagbuffer = REG[arg_a].Val + REG[arg_b].Val;
					if (flagbuffer > 4095)
					{
						Set_Flag(Flags.Overflow);
						REG[arg_b].Val = flagbuffer;
					}
					else
					{
						REG[arg_b].Val = flagbuffer;
					}
					break;
				case 2:
					// Subtract
					flagbuffer = REG[arg_a].Val - REG[arg_b].Val;
					if (flagbuffer > 4095)
					{
						Set_Flag(Flags.Overflow);
						REG[arg_b].Val = flagbuffer;
					}
					else
					{
						REG[arg_b].Val = flagbuffer;
					}
					break;
				case 3:
					// Reversed Subtract
					flagbuffer = REG[arg_b].Val - REG[arg_a].Val;
					if (flagbuffer > 4095)
					{
						Set_Flag(Flags.Overflow);
						REG[arg_b].Val = flagbuffer;
					}
					else
					{
						REG[arg_b].Val = flagbuffer;
					}
					break;
				case 4:
					// AND
					REG[arg_b].Val = REG[arg_a].Val & REG[arg_b].Val;
					break;
				case 5:
					// OR
					REG[arg_b].Val = REG[arg_a].Val | REG[arg_b].Val;
					break;
				case 6:
					// XOR
					REG[arg_b].Val = REG[arg_a].Val ^ REG[arg_b].Val;
					break;
				case 11:
					// Number Comparasion
					CMD_Compare(REG[arg_b].Val, REG[arg_a].Val);
					break;
				case 12:
					// Conditional Move Reg -> Reg
					if (GetFlags((Flags)CheckFlagReg) != 0)
					{
						REG[arg_b].Val = REG[arg_a].Val;
					}
					break;
				case 13:
					// Move Reg -> Reg
					REG[arg_b].Val = REG[arg_a].Val;
					break;
				case 14:
					// Move Reg -> RAM
					RAM[REG[arg_a].Val].Val = REG[arg_b].Val;
					break;
				case 15:
					// Move RAM -> Reg
					REG[arg_b].Val = RAM[REG[arg_a].Val].Val;
					break;
				default:
					Console.WriteLine("Program Error (Invalid Command)");
					Environment.Exit(1);
					break;
			}
		}

		private void ExecuteCommand_L1(uint arg_a, uint arg_b, uint flagbuffer)
		{
			switch (arg_a)
			{
				case 0:
					ExecuteCommand_L2(arg_b, flagbuffer);
					break;
				case 1:
					// Move N1, Reg[b]
					CounterReg++;
					REG[arg_b].Val = RAM[(CounterReg - 1)].Val;
					break;
				case 2:
					// Conditional Move N1, Reg[b]
					CounterReg++;
					if (GetFlags((Flags)CheckFlagReg) != 0)
					{
						REG[arg_b].Val = RAM[(CounterReg - 1)].Val;
					}
					break;
				case 3:
					// Increment Reg[b]
					flagbuffer = REG[arg_b].Val + 1;
					if (flagbuffer > 4095)
					{
						Set_Flag(Flags.Overflow);
						REG[arg_b].Val = flagbuffer;
					}
					else
					{
						REG[arg_b].Val = flagbuffer;
					}
					break;
				case 4:
					// Decrement Reg[b]
					flagbuffer = REG[arg_b].Val - 1;
					if (flagbuffer > 4095)
					{
						Set_Flag(Flags.Overflow);
						REG[arg_b].Val = flagbuffer;
					}
					else
					{
						REG[arg_b].Val = flagbuffer;
					}
					break;
				case 5:
					// Not Reg[b] (done as xor with max Value because you cannot negate uint Values)
					REG[arg_b].Val = REG[arg_b].Val ^ 0xfff;
					break;
				case 6:
					// Right Shift
					flagbuffer = REG[arg_b].Val & 0x1;
					if (flagbuffer == 1)
					{
						Set_Flag(Flags.Overflow);
					}
					REG[arg_b].Val = REG[arg_b].Val >> 1;
					break;
				case 7:
					// User Input Interrupt
					while (true)
					{
						try
						{
							Console.WriteLine("Please Input Data (HEX) for the CPU: ");
							REG[arg_b].Val = UInt32.Parse(Console.ReadLine() ?? throw new Exception(), System.Globalization.NumberStyles.HexNumber);
							break;
						}
						catch (Exception)
						{
							Console.WriteLine("Invalid Value");
						}
					}
					break;
				case 8:
					// Addition N1 + Reg
					CounterReg++;
					flagbuffer = RAM[(CounterReg - 1)].Val + REG[arg_b].Val;
					if (flagbuffer > 4095)
					{
						Set_Flag(Flags.Overflow);
						REG[arg_b].Val = flagbuffer;
					}
					else
					{
						REG[arg_b].Val = flagbuffer;
					}
					break;
				case 9:
					// Subtract N1 - Reg
					CounterReg++;
					flagbuffer = RAM[(CounterReg - 1)].Val - REG[arg_b].Val;
					if (flagbuffer > 4095)
					{
						Set_Flag(Flags.Overflow);
						REG[arg_b].Val = flagbuffer;
					}
					else
					{
						REG[arg_b].Val = flagbuffer;
					}
					break;
				case 10:
					// Reversed Subtract Reg - N1
					CounterReg++;
					flagbuffer = REG[arg_b].Val - RAM[(CounterReg - 1)].Val;
					if (flagbuffer > 4095)
					{
						Set_Flag(Flags.Overflow);
						REG[arg_b].Val = flagbuffer;
					}
					else
					{
						REG[arg_b].Val = flagbuffer;
					}
					break;
				case 11:
					// AND N1 & Reg
					CounterReg++;
					REG[arg_b].Val = RAM[(CounterReg - 1)].Val & REG[arg_b].Val;
					break;
				case 12:
					// OR N1 | Reg
					CounterReg++;
					REG[arg_b].Val = RAM[(CounterReg - 1)].Val | REG[arg_b].Val;
					break;
				case 13:
					// XOR N1 ^ Reg
					CounterReg++;
					REG[arg_b].Val = RAM[(CounterReg - 1)].Val ^ REG[arg_b].Val;
					break;
				case 14:
					// Number Comparasion N1 ? Reg
					CounterReg++;
					CMD_Compare(REG[arg_b].Val, RAM[(CounterReg - 1)].Val);
					break;
				default:
					Console.WriteLine("Program Error (Invalid Command)");
					Environment.Exit(2);
					break;
			}
		}

		private void ExecuteCommand_L2(uint arg_b, uint flagbuffer)
		{
			switch (arg_b)
			{
				case 0:
					// Stop
					_isCpuRunning = false;
					break;
				case 1:
					// Conditional Stop
					if (GetFlags((Flags)CheckFlagReg) != 0)
						_isCpuRunning = false;
					break;
				default:
					Console.WriteLine("Program Error (Invalid Command)");
					System.Environment.Exit(3);
					break;
			}
		}

		private void CMD_Compare(uint B, uint A)
		{
			if (B > A)
			{
				Set_Flag(Flags.BGreater);
			}
			else if (B < A)
			{
				Set_Flag(Flags.AGreater);
			}
			else if (B == A)
			{
				Set_Flag(Flags.Equal);
			}
			else
			{
				Console.WriteLine("Comparation Error");
				Environment.Exit(1);
			}
		}

		private void Set_Flag(Flags flag)
		{
			FlagReg |= (uint)flag;
		}
	}
}
