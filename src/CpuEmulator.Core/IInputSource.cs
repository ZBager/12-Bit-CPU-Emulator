namespace CpuEmulator
{
	/// <summary>
	/// Supplies a value to the CPU's user-input instruction (L1 opcode 7).
	/// The core used to call Console.ReadLine() directly, which blocked the
	/// emulator thread on a console a GUI host may not even have.
	/// </summary>
	public interface IInputSource
	{
		/// <summary>
		/// Blocks until a value is available. Implementations are responsible for
		/// validation and for re-prompting on bad input; only a usable value should
		/// be returned. The result is masked to 12 bits by the caller.
		/// </summary>
		/// <exception cref="OperationCanceledException">
		/// Thrown if <paramref name="cancellationToken"/> is signalled while waiting.
		/// Honouring this is what lets a host stop a CPU that is parked on input.
		/// </exception>
		uint ReadValue(CancellationToken cancellationToken);
	}
}
