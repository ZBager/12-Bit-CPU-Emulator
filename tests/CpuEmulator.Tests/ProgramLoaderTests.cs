namespace CpuEmulator.Tests
{
	public class ProgramLoaderTests
	{
		[Fact]
		public void SkipsCommentsAndBlankLines()
		{
			IReadOnlyList<uint> words = ProgramLoader.Parse(new[]
			{
				"//a comment",
				"110",
				"",
				"   ",
				"//another",
				"030",
			});

			Assert.Equal(new uint[] { 0x110, 0x030 }, words);
		}

		[Fact]
		public void ParsesHexCaseInsensitively()
		{
			Assert.Equal(new uint[] { 0xabc, 0xABC }, ProgramLoader.Parse(new[] { "abc", "ABC" }));
		}

		[Fact]
		public void RejectsNonHexLine_ReportingTheLineNumber()
		{
			ProgramFormatException ex = Assert.Throws<ProgramFormatException>(
				() => ProgramLoader.Parse(new[] { "110", "//ok", "zzz" }));

			Assert.Equal(3, ex.LineNumber);
			Assert.Contains("zzz", ex.Message);
		}

		[Fact]
		public void RejectsWordWiderThanTwelveBits()
		{
			// The old loader silently truncated this via the Data12Bit mask.
			ProgramFormatException ex = Assert.Throws<ProgramFormatException>(
				() => ProgramLoader.Parse(new[] { "1000" }));

			Assert.Equal(1, ex.LineNumber);
		}

		[Fact]
		public void ParseFileThrowsWhenMissing()
		{
			Assert.Throws<FileNotFoundException>(
				() => ProgramLoader.ParseFile(Cpu.DataPath("does-not-exist.txt")));
		}

		[Fact]
		public void ReadsTheRealProgramFiles()
		{
			Assert.Equal(64, ProgramLoader.ParseFile(Cpu.DataPath("program.txt")).Count);
			Assert.Equal(12, ProgramLoader.ParseFile(Cpu.DataPath("program1.txt")).Count);
		}

		[Fact]
		public void LoadProgramRejectsProgramLargerThanRam()
		{
			Emulator cpu = new Emulator();
			Assert.Throws<ArgumentException>(() => cpu.LoadProgram(new uint[4097]));
		}
	}
}
