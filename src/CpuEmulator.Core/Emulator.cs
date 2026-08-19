using System;

namespace CpuEmulator
{
	public class Emulator
	{
		//RAM & REGISTERS data structure
		public Data12Bit[] RAM = new Data12Bit[4096];
		public Data12Bit[] REG = new Data12Bit[16];

		private readonly IInputSource? _input;

		//Address of the instruction currently being decoded, for error reporting
		private uint _currentAddress;

		/// <param name="input">
		/// Supplies values to the user-input instruction (L1 opcode 7). May be left null
		/// if the program being run never uses that instruction; it throws if one does.
		/// </param>
		public Emulator(IInputSource? input = null)
		{
			_input = input;
		}
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
		/// <summary>
		/// Writes a parsed program into RAM starting at address 0. Use
		/// <see cref="ProgramLoader"/> to turn a file or listing into words.
		/// Does not clear the rest of RAM first.
		/// </summary>
		/// <exception cref="ArgumentException">The program is larger than RAM.</exception>
		public void LoadProgram(IReadOnlyList<uint> program)
		{
			ArgumentNullException.ThrowIfNull(program);

			if (program.Count > RAM.Length)
				throw new ArgumentException(
					$"Program is {program.Count} words but RAM holds {RAM.Length}.", nameof(program));

			for (int i = 0; i < program.Count; i++)
				RAM[i].Val = program[i];
		}

		/// <summary>
		/// Clears RAM, clears the registers, and puts the CPU back into the running
		/// state so it can be started again after a program has halted.
		/// </summary>
		public void Reset()
		{
			Array.Clear(RAM);
			Array.Clear(REG);
			_isCpuRunning = true;
		}
		/// <summary>Renders RAM as 16 words per line. Returns the text rather than writing to a console.</summary>
		public string DumpRam()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.AppendLine("RAM Values:");
			for (int i = 0; i < RAM.Length; i += 16)
			{
				sb.Append("0x" + i.ToString("X3") + ": ");
				for (int j = 0; j < 16; j++)
				{
					sb.Append(RAM[i + j].Val.ToString("X3") + " ");
				}
				sb.AppendLine();
			}
			return sb.ToString();
		}

		/// <summary>Renders the 16 registers on one line.</summary>
		public string DumpRegisters()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.AppendLine("Register Values:");
			sb.Append("0x0:   ");
			for (int i = 0; i < 16; i++)
			{
				sb.Append(REG[i].Val.ToString("X3") + " ");
			}
			sb.AppendLine();
			return sb.ToString();
		}

		/// <summary>Renders the flags currently set in register 14.</summary>
		public string DumpFlags()
		{
			return GetFlags(Flags.All).ToString();
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
		/// <summary>
		/// Fetches, decodes and executes one instruction.
		/// </summary>
		/// <param name="cancellationToken">
		/// Observed only while the CPU is blocked on the user-input instruction. A host
		/// that wants to stop a running CPU otherwise simply stops calling this method.
		/// </param>
		/// <exception cref="InvalidOpcodeException">The word at the program counter does not decode.</exception>
		public void NextCommand(CancellationToken cancellationToken = default)
		{
			_currentAddress = CounterReg;
			uint opcode = RAM[CounterReg].Val;
			uint instruction = opcode & 0xf;
			uint arg_a = (opcode >> 4) & 0xf;
			uint arg_b = (opcode >> 8) & 0xf;
			CounterReg++;
			ExecuteCommand_L0(instruction, arg_a, arg_b, cancellationToken);
		}

		private uint CurrentWord => RAM[_currentAddress].Val;


		private void ExecuteCommand_L0(uint instruction, uint arg_a, uint arg_b, CancellationToken cancellationToken)
		{
			switch (instruction)
			{
				case 0:
					ExecuteCommand_L1(arg_a, arg_b, cancellationToken);
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
					ALU_Compare(REG[arg_b], REG[arg_a]);
					break;
				case 12:
					if (CPU_CheckCondition())
						CPU_Move(ref REG[arg_b], REG[arg_a]);
					break;
				case 13:
					CPU_Move(ref REG[arg_b], REG[arg_a]);
					break;
				case 14:
					CPU_Move(ref RAM[REG[arg_a].Val], REG[arg_b]);
					break;
				case 15:
					CPU_Move(ref REG[arg_b], RAM[REG[arg_a].Val]);
					break;
				default:
					throw new InvalidOpcodeException(0, _currentAddress, CurrentWord);
			}
		}
		private void ExecuteCommand_L1(uint arg_a, uint arg_b, CancellationToken cancellationToken)
		{
			switch (arg_a)
			{
				case 0:
					ExecuteCommand_L2(arg_b);
					break;
				case 1:
					CounterReg++;
					CPU_Move(ref REG[arg_b], RAM[(CounterReg - 1)]);
					break;
				case 2:
					CounterReg++;
					if (CPU_CheckCondition())
						CPU_Move(ref REG[arg_b], RAM[(CounterReg - 1)]);
					break;
				case 3:
					ALU_INC(ref REG[arg_b]);
					break;
				case 4:
					ALU_DEC(ref REG[arg_b]);
					break;
				case 5:
					ALU_NOT(ref REG[arg_b]);
					break;
				case 6:
					ALU_RSH(ref REG[arg_b]);
					break;
				case 7:
					// User Input Interrupt. Prompting, validation and retry are the host's
					// job; the CPU just blocks until a usable value comes back.
					if (_input is null)
						throw new InvalidOperationException(
							$"The program executed the user-input instruction at address 0x{_currentAddress:X3}, " +
							$"but this {nameof(Emulator)} was constructed without an {nameof(IInputSource)}.");
					REG[arg_b].Val = _input.ReadValue(cancellationToken);
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
					ALU_Compare(REG[arg_b], RAM[(CounterReg - 1)]);
					break;
				default:
					throw new InvalidOpcodeException(1, _currentAddress, CurrentWord);
			}
		}
		private void ExecuteCommand_L2(uint arg_b)
		{
			switch (arg_b)
			{
				case 0:
					CPU_Stop();
					break;
				case 1:
					if (CPU_CheckCondition())
						CPU_Stop();
					break;
				default:
					throw new InvalidOpcodeException(2, _currentAddress, CurrentWord);
			}
		}


		private bool CPU_CheckCondition()
		{
			if (GetFlags((Flags)CheckFlagReg) != 0)
				return true;
			return false;
		}
		private void CPU_Stop()
		{
			_isCpuRunning = false;
		}
		private void CPU_Move(ref Data12Bit B, Data12Bit A)
		{
			B.Val = A.Val;
		}
		private void ALU_CheckOverflow(uint value)
		{
			if (value > 4095)
				Set_Flag(Flags.Overflow);
		}
		private void ALU_Compare(Data12Bit B, Data12Bit A)
		{
			if (B.Val > A.Val)
			{
				Set_Flag(Flags.BGreater);
			}
			else if (B.Val < A.Val)
			{
				Set_Flag(Flags.AGreater);
			}
			else
			{
				//B.Val == A.Val is all that remains
				Set_Flag(Flags.Equal);
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
		private void ALU_RSH(ref Data12Bit B)
		{
			if ((B.Val & 0x1) == 1)
				Set_Flag(Flags.Overflow);
			B.Val = B.Val >> 1;
		}
		private void ALU_NOT(ref Data12Bit B)
		{
			B.Val = B.Val ^ 0xfff;
		}
		private void Set_Flag(Flags flag)
		{
			FlagReg |= (uint)flag;
		}
	}
}