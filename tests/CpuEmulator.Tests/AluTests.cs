namespace CpuEmulator.Tests
{
	/// <summary>
	/// Arithmetic, logic and flag behavior. Operand order matters a lot here and is
	/// easy to "fix" by accident, so each case states the intended direction.
	/// </summary>
	public class AluTests
	{
		private const uint A = 1;   // source register
		private const uint B = 2;   // destination register

		private static Emulator Run(uint instruction, uint a, uint b)
		{
			Emulator cpu = Cpu.Load(Cpu.W(B, A, instruction));
			cpu.REG[A].Val = a;
			cpu.REG[B].Val = b;
			cpu.NextCommand();
			return cpu;
		}

		[Fact]
		public void Add_WritesSumIntoB()
		{
			Assert.Equal(0x00Cu, Run(1, 0x005, 0x007).REG[B].Val);
		}

		[Fact]
		public void Sub_ComputesAMinusB_NotBMinusA()
		{
			// Intentional and confirmed by the author: "Sub A, B" means B becomes A - B.
			// Rsub is the variant that computes B - A. Do not "correct" this.
			Assert.Equal(0x002u, Run(2, 0x005, 0x003).REG[B].Val);
		}

		[Fact]
		public void Rsub_ComputesBMinusA()
		{
			Assert.Equal(0x002u, Run(3, 0x003, 0x005).REG[B].Val);
		}

		[Theory]
		[InlineData(4u, 0b1100u, 0b1010u, 0b1000u)]   // AND
		[InlineData(5u, 0b1100u, 0b1010u, 0b1110u)]   // OR
		[InlineData(6u, 0b1100u, 0b1010u, 0b0110u)]   // XOR
		public void Bitwise_CombinesIntoB(uint instruction, uint a, uint b, uint expected)
		{
			Assert.Equal(expected, Run(instruction, a, b).REG[B].Val);
		}

		[Fact]
		public void Add_WrapsAtTwelveBitsAndSetsOverflow()
		{
			Emulator cpu = Run(1, 0xfff, 0x001);
			Assert.Equal(0x000u, cpu.REG[B].Val);
			Assert.Equal((uint)Emulator.Flags.Overflow, cpu.REG[Cpu.Flags].Val);
		}

		[Fact]
		public void Sub_ThatBorrows_WrapsAndSetsOverflow()
		{
			// Overflow is checked against the unmasked uint result, so any borrow
			// underflows past uint.MaxValue and always trips the flag.
			// Current behavior, asserted so a change is deliberate rather than silent.
			Emulator cpu = Run(2, 0x001, 0x003);
			Assert.Equal(0xffeu, cpu.REG[B].Val);
			Assert.Equal((uint)Emulator.Flags.Overflow, cpu.REG[Cpu.Flags].Val);
		}

		[Theory]
		[InlineData(0x005u, 0x003u, Emulator.Flags.AGreater)]
		[InlineData(0x003u, 0x005u, Emulator.Flags.BGreater)]
		[InlineData(0x004u, 0x004u, Emulator.Flags.Equal)]
		public void Compare_SetsTheFlagDescribingBRelativeToA(uint a, uint b, Emulator.Flags expected)
		{
			Emulator cpu = Run(11, a, b);
			Assert.Equal((uint)expected, cpu.REG[Cpu.Flags].Val);
		}

		[Fact]
		public void Compare_LeavesOperandsAlone()
		{
			Emulator cpu = Run(11, 0x005, 0x003);
			Assert.Equal(0x005u, cpu.REG[A].Val);
			Assert.Equal(0x003u, cpu.REG[B].Val);
		}

		[Fact]
		public void Flags_AccumulateAndAreNeverClearedByTheCpu()
		{
			// Set_Flag only ORs; a program clears flags by writing 0 to REG[14] itself.
			Emulator cpu = Cpu.Load(
				Cpu.W(B, A, 11),    // compare -> Equal
				Cpu.L1(B, 6));      // Rsh with low bit set -> Overflow
			//Equal so Compare sets Equal, and odd so the shift drops a bit and sets Overflow.
			cpu.REG[A].Val = 0x005;
			cpu.REG[B].Val = 0x005;

			cpu.NextCommand();
			cpu.NextCommand();

			Assert.Equal(
				(uint)(Emulator.Flags.Equal | Emulator.Flags.Overflow),
				cpu.REG[Cpu.Flags].Val);
		}

		[Fact]
		public void Increment_WrapsFromMaxAndSetsOverflow()
		{
			Emulator cpu = Cpu.Load(Cpu.L1(B, 3));
			cpu.REG[B].Val = 0xfff;
			cpu.NextCommand();

			Assert.Equal(0x000u, cpu.REG[B].Val);
			Assert.Equal((uint)Emulator.Flags.Overflow, cpu.REG[Cpu.Flags].Val);
		}

		[Fact]
		public void Decrement_WrapsFromZeroAndSetsOverflow()
		{
			Emulator cpu = Cpu.Load(Cpu.L1(B, 4));
			cpu.REG[B].Val = 0x000;
			cpu.NextCommand();

			Assert.Equal(0xfffu, cpu.REG[B].Val);
			Assert.Equal((uint)Emulator.Flags.Overflow, cpu.REG[Cpu.Flags].Val);
		}

		[Fact]
		public void Not_InvertsAllTwelveBits()
		{
			Emulator cpu = Cpu.Load(Cpu.L1(B, 5));
			cpu.REG[B].Val = 0b0000_1111_0000;
			cpu.NextCommand();

			Assert.Equal(0b1111_0000_1111u, cpu.REG[B].Val);
		}

		[Fact]
		public void RightShift_SetsOverflowOnlyWhenABitIsShiftedOut()
		{
			Emulator even = Cpu.Load(Cpu.L1(B, 6));
			even.REG[B].Val = 0b100;
			even.NextCommand();
			Assert.Equal(0b010u, even.REG[B].Val);
			Assert.Equal((uint)Emulator.Flags.None, even.REG[Cpu.Flags].Val);

			Emulator odd = Cpu.Load(Cpu.L1(B, 6));
			odd.REG[B].Val = 0b101;
			odd.NextCommand();
			Assert.Equal(0b010u, odd.REG[B].Val);
			Assert.Equal((uint)Emulator.Flags.Overflow, odd.REG[Cpu.Flags].Val);
		}
	}
}
