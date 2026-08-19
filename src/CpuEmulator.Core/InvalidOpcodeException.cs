using System.Globalization;

namespace CpuEmulator
{
	/// <summary>
	/// Thrown when the decoder reaches an opcode that has no instruction behind it.
	/// Replaces the Environment.Exit(1|2|3) calls the emulator used to make, which
	/// killed the host process without a message.
	/// </summary>
	public class InvalidOpcodeException : Exception
	{
		public InvalidOpcodeException(int decodeLevel, uint address, uint word)
			: base(string.Format(
				CultureInfo.InvariantCulture,
				"Invalid opcode 0x{0:X3} at address 0x{1:X3} (rejected at decode level L{2}).",
				word, address, decodeLevel))
		{
			DecodeLevel = decodeLevel;
			Address = address;
			Word = word;
		}

		/// <summary>Which decode tier rejected the word: 0, 1 or 2.</summary>
		public int DecodeLevel { get; }

		/// <summary>Address of the instruction word that failed to decode.</summary>
		public uint Address { get; }

		/// <summary>The raw 12-bit word that failed to decode.</summary>
		public uint Word { get; }
	}
}
