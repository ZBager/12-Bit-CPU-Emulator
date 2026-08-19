using CpuEmulator;

namespace CpuEmulator.Tests
{
	/// <summary>
	/// Helpers for assembling instruction words by hand and running them.
	/// A word is three nibbles: (arg_b &lt;&lt; 8) | (arg_a &lt;&lt; 4) | instruction.
	/// </summary>
	internal static class Cpu
	{
		/// <summary>An L0 instruction, where both arguments are register addresses.</summary>
		public static uint W(uint argB, uint argA, uint instruction)
			=> (argB << 8) | (argA << 4) | instruction;

		/// <summary>An L1 instruction (instruction nibble 0), where arg_a carries the opcode.</summary>
		public static uint L1(uint argB, uint opcode) => W(argB, opcode, 0);

		/// <summary>An L2 instruction (instruction and arg_a both 0), where arg_b carries the opcode.</summary>
		public static uint L2(uint opcode) => W(opcode, 0, 0);

		public const uint Stop = 0x000;

		/// <summary>Register indices the CPU treats as architectural.</summary>
		public const uint CheckMask = 13;
		public const uint Flags = 14;
		public const uint Pc = 15;

		public static Emulator Load(params uint[] program)
		{
			Emulator cpu = new Emulator();
			cpu.LoadProgram(program);
			return cpu;
		}

		/// <summary>Runs to a halt, failing rather than hanging if the program never stops.</summary>
		public static int RunToHalt(Emulator cpu, int maxSteps = 2_000_000)
		{
			int steps = 0;
			while (cpu.IsRunning())
			{
				if (steps >= maxSteps)
					throw new InvalidOperationException($"Program did not halt within {maxSteps} instructions.");
				cpu.NextCommand();
				steps++;
			}
			return steps;
		}

		/// <summary>Locates a file copied next to the test assembly.</summary>
		public static string DataPath(string name)
			=> Path.Combine(AppContext.BaseDirectory, "data", name);

		public static string GoldenPath(string name)
			=> Path.Combine(AppContext.BaseDirectory, "Golden", name);

		/// <summary>Line-ending-insensitive comparison, so golden files match on Linux and Windows alike.</summary>
		public static string Normalize(string text)
			=> text.Replace("\r\n", "\n").TrimEnd('\n');
	}

	/// <summary>An <see cref="IInputSource"/> that replays a fixed queue of values.</summary>
	internal sealed class ScriptedInput : IInputSource
	{
		private readonly Queue<uint> _values;

		public ScriptedInput(params uint[] values) => _values = new Queue<uint>(values);

		public int Remaining => _values.Count;

		public uint ReadValue(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (_values.Count == 0)
				throw new InvalidOperationException("The program asked for more input than the script provides.");
			return _values.Dequeue();
		}
	}

	/// <summary>An <see cref="IInputSource"/> that blocks until cancelled, to test Stop-while-waiting.</summary>
	internal sealed class BlockingInput : IInputSource
	{
		public uint ReadValue(CancellationToken cancellationToken)
		{
			cancellationToken.WaitHandle.WaitOne();
			cancellationToken.ThrowIfCancellationRequested();
			return 0;
		}
	}
}
