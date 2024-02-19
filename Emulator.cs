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
			Console.WriteLine(GetFlags(Flags.All));
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
			uint opcode = RAM[CounterReg].Val;
			uint instruction = opcode & 0xf;
			uint arg_a = (opcode >> 4) & 0xf;
			uint arg_b = (opcode >> 8) & 0xf;
			CounterReg++;
			ExecuteCommand_L0(instruction, arg_a, arg_b);
		}

		private void ExecuteCommand_L0(uint instruction, uint arg_a, uint arg_b)
		{
			switch (instruction)
			{
				case 0:
					ExecuteCommand_L1(arg_a, arg_b);
					break;
				case 1:
					ALU_Addition(ref REG[arg_b], REG[arg_a]);
					break;
				case 2:
					ALU_Subraction(ref REG[arg_b], REG[arg_a]);
					break;
				case 3:
					ALU_ReversedSubraction(ref REG[arg_b], REG[arg_a]);
					break;
				case 4:
					ALU_AND(ref REG[arg_b], REG[arg_a]);
					break;
				case 5:
					ALU_OR(ref REG[arg_b], REG[arg_a]);
					break;
				case 6:
					ALU_XOR(ref REG[arg_b], REG[arg_a]);
					break;
				case 11:
					ALU_Compare(REG[arg_b].Val, REG[arg_a].Val);
					break;
				case 12:
					// Conditional Move Reg -> Reg
					if (CPU_CheckCondition())
						REG[arg_b].Val = REG[arg_a].Val;
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

		private void ExecuteCommand_L1(uint arg_a, uint arg_b)
		{
			switch (arg_a)
			{
				case 0:
					ExecuteCommand_L2(arg_b);
					break;
				case 1:
					// Move N1, Reg[b]
					CounterReg++;
					REG[arg_b].Val = RAM[(CounterReg - 1)].Val;
					break;
				case 2:
					// Conditional Move N1, Reg[b]
					CounterReg++;
					if (CPU_CheckCondition())
						REG[arg_b].Val = RAM[(CounterReg - 1)].Val;
					break;
				case 3:
					ALU_INC(ref REG[arg_b]);
					break;
				case 4:
					ALU_DEC(ref REG[arg_b]);
					break;
				case 5:
					// Not Reg[b] (done as xor with max Value because you cannot negate uint Values)
					REG[arg_b].Val = REG[arg_b].Val ^ 0xfff;
					break;
				case 6:
					// Right Shift
					if ((REG[arg_b].Val & 0x1) == 1)
						Set_Flag(Flags.Overflow);
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
					CounterReg++;
					ALU_Addition(ref REG[arg_b], RAM[(CounterReg - 1)]);
					break;
				case 9:
					CounterReg++;
					ALU_Subraction(ref REG[arg_b], RAM[(CounterReg - 1)]);
					break;
				case 10:
					CounterReg++;
					ALU_ReversedSubraction(ref REG[arg_b], RAM[(CounterReg - 1)]);
					break;
				case 11:
					CounterReg++;
					ALU_AND(ref REG[arg_b], RAM[(CounterReg - 1)]);
					break;
				case 12:
					CounterReg++;
					ALU_OR(ref REG[arg_b], RAM[(CounterReg - 1)]);
					break;
				case 13:
					CounterReg++;
					ALU_XOR(ref REG[arg_b], RAM[(CounterReg - 1)]);
					break;
				case 14:
					CounterReg++;
					ALU_Compare(REG[arg_b].Val, RAM[(CounterReg - 1)].Val);
					break;
				default:
					Console.WriteLine("Program Error (Invalid Command)");
					Environment.Exit(2);
					break;
			}
		}

		private void ExecuteCommand_L2(uint arg_b)
		{
			switch (arg_b)
			{
				case 0:
					// Stop
					_isCpuRunning = false;
					break;
				case 1:
					// Conditional Stop
					if (CPU_CheckCondition())
						_isCpuRunning = false;
					break;
				default:
					Console.WriteLine("Program Error (Invalid Command)");
					System.Environment.Exit(3);
					break;
			}
		}
		private bool CPU_CheckCondition()
		{
			if (GetFlags((Flags)CheckFlagReg) != 0)
				return true;
			return false;
		}

		private void ALU_CheckOverflow(uint value)
		{
			if (value > 4095)
				Set_Flag(Flags.Overflow);
		}

		private void ALU_Compare(uint B, uint A)
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

		private void ALU_Addition(ref Data12Bit B, Data12Bit A)
		{
			ALU_CheckOverflow(A.Val + B.Val);
			B.Val = A.Val + B.Val;
		}

		private void ALU_Subraction(ref Data12Bit B, Data12Bit A)
		{
			ALU_CheckOverflow(A.Val - B.Val);
			B.Val = A.Val - B.Val;
		}

		private void ALU_ReversedSubraction(ref Data12Bit B, Data12Bit A)
		{
			ALU_CheckOverflow(B.Val - A.Val);
			B.Val = B.Val - A.Val;
		}

		private void ALU_AND(ref Data12Bit B, Data12Bit A)
		{
			B.Val = B.Val & A.Val;
		}

		private void ALU_OR(ref Data12Bit B, Data12Bit A)
		{
			B.Val = B.Val | A.Val;
		}

		private void ALU_XOR(ref Data12Bit B, Data12Bit A)
		{
			B.Val = B.Val ^ A.Val;
		}

		private void ALU_INC(ref Data12Bit B)
		{
			ALU_CheckOverflow(B.Val + 1);
			B.Val++;
		}

		private void ALU_DEC(ref Data12Bit B)
		{
			ALU_CheckOverflow(B.Val - 1);
			B.Val--;
		}

		private void Set_Flag(Flags flag)
		{
			FlagReg |= (uint)flag;
		}
	}
}
