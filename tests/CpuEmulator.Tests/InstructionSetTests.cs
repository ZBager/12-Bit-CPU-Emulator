namespace CpuEmulator.Tests
{
	/// <summary>
	/// Decoding, data movement, immediates, halting and the error paths.
	/// </summary>
	public class InstructionSetTests
	{
		private const uint A = 1;
		private const uint B = 2;

		[Fact]
		public void Move_CopiesAIntoB()
		{
			Emulator cpu = Cpu.Load(Cpu.W(B, A, 13));
			cpu.REG[A].Val = 0x0ab;
			cpu.NextCommand();

			Assert.Equal(0x0abu, cpu.REG[B].Val);
			Assert.Equal(0x0abu, cpu.REG[A].Val);
		}

		[Fact]
		public void Opcode14_StoresBIntoMemoryAtA()
		{
			// The README has E and F the other way round; the code is authoritative.
			Emulator cpu = Cpu.Load(Cpu.W(B, A, 14));
			cpu.REG[A].Val = 0x100;
			cpu.REG[B].Val = 0x0cd;
			cpu.NextCommand();

			Assert.Equal(0x0cdu, cpu.RAM[0x100].Val);
		}

		[Fact]
		public void Opcode15_LoadsMemoryAtAIntoB()
		{
			Emulator cpu = Cpu.Load(Cpu.W(B, A, 15));
			cpu.REG[A].Val = 0x100;
			cpu.RAM[0x100].Val = 0x0ef;
			cpu.NextCommand();

			Assert.Equal(0x0efu, cpu.REG[B].Val);
		}

		[Fact]
		public void Immediate_TakesTheFollowingWordAndAdvancesThePcByTwo()
		{
			Emulator cpu = Cpu.Load(Cpu.L1(B, 1), 0x0ab);
			cpu.NextCommand();

			Assert.Equal(0x0abu, cpu.REG[B].Val);
			Assert.Equal(2u, cpu.REG[Cpu.Pc].Val);
		}

		[Theory]
		[InlineData(8u, 0x005u, 0x003u, 0x008u)]   // Add  imm -> B = imm + B
		[InlineData(9u, 0x005u, 0x003u, 0x002u)]   // Sub  imm -> B = imm - B
		[InlineData(10u, 0x005u, 0x003u, 0xffeu)]  // Rsub imm -> B = B - imm
		[InlineData(11u, 0b1100u, 0b1010u, 0b1000u)]
		[InlineData(12u, 0b1100u, 0b1010u, 0b1110u)]
		[InlineData(13u, 0b1100u, 0b1010u, 0b0110u)]
		public void ImmediateAlu_UsesTheFollowingWordAsTheOperand(
			uint opcode, uint immediate, uint b, uint expected)
		{
			Emulator cpu = Cpu.Load(Cpu.L1(B, opcode), immediate);
			cpu.REG[B].Val = b;
			cpu.NextCommand();

			Assert.Equal(expected, cpu.REG[B].Val);
			Assert.Equal(2u, cpu.REG[Cpu.Pc].Val);
		}

		[Fact]
		public void JumpIsJustAWriteToRegister15()
		{
			Emulator cpu = Cpu.Load(Cpu.L1(Cpu.Pc, 1), 0x040);
			cpu.NextCommand();

			Assert.Equal(0x040u, cpu.REG[Cpu.Pc].Val);
		}

		[Fact]
		public void ConditionalMove_RunsOnlyWhenTheMaskedFlagsAreNonZero()
		{
			// The condition is (REG[13] & REG[14]) != 0: a program selects which
			// flags it cares about by writing them into REG[13] first.
			Emulator taken = Cpu.Load(Cpu.W(B, A, 12));
			taken.REG[A].Val = 0x0ff;
			taken.REG[Cpu.Flags].Val = (uint)Emulator.Flags.Equal;
			taken.REG[Cpu.CheckMask].Val = (uint)Emulator.Flags.Equal;
			taken.NextCommand();
			Assert.Equal(0x0ffu, taken.REG[B].Val);

			Emulator skipped = Cpu.Load(Cpu.W(B, A, 12));
			skipped.REG[A].Val = 0x0ff;
			skipped.REG[Cpu.Flags].Val = (uint)Emulator.Flags.Equal;
			skipped.REG[Cpu.CheckMask].Val = (uint)Emulator.Flags.AGreater;
			skipped.NextCommand();
			Assert.Equal(0x000u, skipped.REG[B].Val);
		}

		[Fact]
		public void ConditionalImmediate_ConsumesItsOperandEvenWhenNotTaken()
		{
			// The PC is advanced before the condition is checked, so a skipped
			// conditional load must not leave the CPU executing its own operand.
			Emulator cpu = Cpu.Load(Cpu.L1(B, 2), 0x0ab);
			cpu.NextCommand();

			Assert.Equal(0x000u, cpu.REG[B].Val);
			Assert.Equal(2u, cpu.REG[Cpu.Pc].Val);
		}

		[Fact]
		public void Stop_HaltsTheCpu()
		{
			Emulator cpu = Cpu.Load(Cpu.Stop);
			Assert.True(cpu.IsRunning());
			cpu.NextCommand();
			Assert.False(cpu.IsRunning());
		}

		[Fact]
		public void ConditionalStop_HonoursTheMask()
		{
			Emulator running = Cpu.Load(Cpu.L2(1));
			running.NextCommand();
			Assert.True(running.IsRunning());

			Emulator halted = Cpu.Load(Cpu.L2(1));
			halted.REG[Cpu.Flags].Val = (uint)Emulator.Flags.Equal;
			halted.REG[Cpu.CheckMask].Val = (uint)Emulator.Flags.Equal;
			halted.NextCommand();
			Assert.False(halted.IsRunning());
		}

		[Theory]
		[InlineData(0x007u, 0)]   // L0: instruction nibble 7 is undefined
		[InlineData(0x0f0u, 1)]   // L1: arg_a 15 is undefined
		[InlineData(0x200u, 2)]   // L2: arg_b 2 is undefined
		public void InvalidOpcode_ThrowsWithItsDecodeLevelAndAddress(uint word, int expectedLevel)
		{
			Emulator cpu = Cpu.Load(word);

			InvalidOpcodeException ex = Assert.Throws<InvalidOpcodeException>(() => cpu.NextCommand());

			Assert.Equal(expectedLevel, ex.DecodeLevel);
			Assert.Equal(0u, ex.Address);
			Assert.Equal(word, ex.Word);
		}

		[Fact]
		public void UserInput_TakesItsValueFromTheInputSource()
		{
			ScriptedInput input = new ScriptedInput(0xabc);
			Emulator cpu = new Emulator(input);
			cpu.LoadProgram(new[] { Cpu.L1(B, 7) });
			cpu.NextCommand();

			Assert.Equal(0xabcu, cpu.REG[B].Val);
			Assert.Equal(0, input.Remaining);
		}

		[Fact]
		public void UserInput_WithoutAnInputSource_ThrowsRatherThanHanging()
		{
			Emulator cpu = Cpu.Load(Cpu.L1(B, 7));
			Assert.Throws<InvalidOperationException>(() => cpu.NextCommand());
		}

		[Fact]
		public void UserInput_CanBeCancelled_SoStopWorksOnAParkedCpu()
		{
			// This is the Phase 4 requirement: cooperative cancellation cannot
			// interrupt a blocked wait unless the token reaches the input source.
			using CancellationTokenSource cts = new CancellationTokenSource();
			Emulator cpu = new Emulator(new BlockingInput());
			cpu.LoadProgram(new[] { Cpu.L1(B, 7) });

			Task task = Task.Run(() => cpu.NextCommand(cts.Token));
			cts.CancelAfter(TimeSpan.FromMilliseconds(50));

			Exception? ex = Record.Exception(() => task.Wait(TimeSpan.FromSeconds(10)));

			Assert.IsType<AggregateException>(ex);
			Assert.IsAssignableFrom<OperationCanceledException>(((AggregateException)ex).InnerException);
		}

		[Fact]
		public void Reset_ClearsStateAndMakesTheCpuRunnableAgain()
		{
			Emulator cpu = Cpu.Load(Cpu.Stop);
			cpu.REG[B].Val = 0x0ff;
			cpu.NextCommand();
			Assert.False(cpu.IsRunning());

			cpu.Reset();

			Assert.True(cpu.IsRunning());
			Assert.Equal(0x000u, cpu.REG[B].Val);
			Assert.Equal(0x000u, cpu.REG[Cpu.Pc].Val);
			Assert.Equal(0x000u, cpu.RAM[0].Val);
		}

		[Fact]
		public void LoadProgram_DoesNotClearRamFirst()
		{
			// Documented behavior that programs may rely on; asserted so it stays deliberate.
			Emulator cpu = new Emulator();
			cpu.RAM[0x500].Val = 0x0aa;
			cpu.LoadProgram(new uint[] { Cpu.Stop });

			Assert.Equal(0x0aau, cpu.RAM[0x500].Val);
		}
	}
}
