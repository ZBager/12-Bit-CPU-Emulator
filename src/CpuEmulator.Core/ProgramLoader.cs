using System.Globalization;

namespace CpuEmulator
{
	/// <summary>
	/// Thrown when a program listing cannot be read as 12-bit words.
	/// </summary>
	public class ProgramFormatException : Exception
	{
		public ProgramFormatException(int lineNumber, string message)
			: base($"Line {lineNumber}: {message}")
		{
			LineNumber = lineNumber;
		}

		/// <summary>1-based line number in the source listing.</summary>
		public int LineNumber { get; }
	}

	/// <summary>
	/// Turns a program listing into 12-bit words. Parsing is a pure function over
	/// lines so it can be tested without touching a filesystem; <see cref="ParseFile"/>
	/// is the only part that does I/O.
	/// </summary>
	public static class ProgramLoader
	{
		/// <summary>
		/// Parses a listing: one 3-digit hex word per line, lines starting with "//"
		/// are comments, blank lines are skipped.
		/// </summary>
		/// <exception cref="ProgramFormatException">A line is neither a comment nor a valid 12-bit word.</exception>
		public static IReadOnlyList<uint> Parse(IEnumerable<string> lines)
		{
			ArgumentNullException.ThrowIfNull(lines);

			List<uint> words = new List<uint>();
			int lineNumber = 0;

			foreach (string line in lines)
			{
				lineNumber++;

				if (line is null || string.IsNullOrWhiteSpace(line) || line.StartsWith("//", StringComparison.Ordinal))
					continue;

				if (!uint.TryParse(line, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint value))
					throw new ProgramFormatException(lineNumber, $"'{line.Trim()}' is not a hexadecimal number.");

				if (value > 0xfff)
					throw new ProgramFormatException(lineNumber, $"0x{value:X} does not fit in 12 bits.");

				words.Add(value);
			}

			return words;
		}

		/// <summary>
		/// Reads and parses a listing from disk. Unlike the previous implementation this
		/// resolves the path normally rather than against the assembly location, so a
		/// relative path is relative to the working directory on every platform.
		/// </summary>
		/// <exception cref="FileNotFoundException">The file does not exist.</exception>
		public static IReadOnlyList<uint> ParseFile(string path)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(path);

			string fullPath = Path.GetFullPath(path);
			if (!File.Exists(fullPath))
				throw new FileNotFoundException($"Program file not found: {fullPath}", fullPath);

			return Parse(File.ReadAllLines(fullPath));
		}
	}
}
