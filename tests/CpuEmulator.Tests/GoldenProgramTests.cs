namespace CpuEmulator.Tests
{
	/// <summary>
	/// End-to-end regression tests over the real programs in data/. These are the
	/// net that lets the UI be rewritten without silently changing the CPU: if a
	/// refactor alters behavior at all, the instruction count or the RAM snapshot moves.
	/// </summary>
	public class GoldenProgramTests
	{
		/// <summary>Instruction count measured before the migration began, against the original WPF-era core.</summary>
		private const int ExpectedSortSteps = 1655;

		private const uint DataStart = 0x30;
		private const uint DataEnd = 0x3f;

		[Fact]
		public void BubbleSort_HaltsAfterExactlyTheBaselineNumberOfInstructions()
		{
			Emulator cpu = new Emulator();
			cpu.LoadProgram(ProgramLoader.ParseFile(Cpu.DataPath("program.txt")));

			int steps = Cpu.RunToHalt(cpu);

			Assert.Equal(ExpectedSortSteps, steps);
			Assert.False(cpu.IsRunning());
		}

		[Fact]
		public void BubbleSort_SortsItsArrayAscending()
		{
			Emulator cpu = new Emulator();
			cpu.LoadProgram(ProgramLoader.ParseFile(Cpu.DataPath("program.txt")));

			uint[] before = Region(cpu);
			Cpu.RunToHalt(cpu);
			uint[] after = Region(cpu);

			Assert.Equal(before.OrderBy(v => v).ToArray(), after);
			Assert.Equal(after.OrderBy(v => v).ToArray(), after);
			Assert.Equal(
				new uint[] { 0x010, 0x020, 0x030, 0x040, 0x050, 0x060, 0x070, 0x080,
							 0x090, 0x100, 0x110, 0x120, 0x130, 0x140, 0x150, 0x160 },
				after);
		}

		[Fact]
		public void BubbleSort_LeavesRamMatchingTheGoldenSnapshot()
		{
			// The strongest assertion available: every one of the 4096 words.
			// Regenerate deliberately if behavior is meant to change.
			Emulator cpu = new Emulator();
			cpu.LoadProgram(ProgramLoader.ParseFile(Cpu.DataPath("program.txt")));
			Cpu.RunToHalt(cpu);

			string expected = Cpu.Normalize(File.ReadAllText(Cpu.GoldenPath("program.ram.txt")));
			string actual = Cpu.Normalize(cpu.DumpRam());

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void ProgramOne_RunsToCompletionOnScriptedInput()
		{
			ScriptedInput input = new ScriptedInput(0xabc);
			Emulator cpu = new Emulator(input);
			cpu.LoadProgram(ProgramLoader.ParseFile(Cpu.DataPath("program1.txt")));

			int steps = Cpu.RunToHalt(cpu);

			Assert.Equal(16323, steps);
			Assert.Equal(0xabcu, cpu.REG[2].Val);
			Assert.Equal(0, input.Remaining);
		}

		private static uint[] Region(Emulator cpu)
		{
			List<uint> values = new List<uint>();
			for (uint i = DataStart; i <= DataEnd; i++)
				values.Add(cpu.RAM[i].Val);
			return values.ToArray();
		}
	}
}
