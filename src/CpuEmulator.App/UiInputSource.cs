using Avalonia.Controls;
using Avalonia.Threading;

namespace CpuEmulator.App
{
	/// <summary>
	/// Feeds the CPU's user-input instruction from a modal dialog.
	/// <para>
	/// <see cref="ReadValue"/> blocks its calling thread on purpose — the CPU is parked
	/// until a value arrives — so it must never be called on the UI thread, or the wait
	/// would deadlock against the dialog it is waiting for. Every path that steps the
	/// CPU therefore runs on a background thread.
	/// </para>
	/// </summary>
	public sealed class UiInputSource : IInputSource
	{
		private readonly Window _owner;

		public UiInputSource(Window owner) => _owner = owner;

		public uint ReadValue(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			TaskCompletionSource<uint> completion =
				new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);
			InputDialog? dialog = null;

			Dispatcher.UIThread.Post(async () =>
			{
				try
				{
					if (cancellationToken.IsCancellationRequested)
					{
						completion.TrySetCanceled(cancellationToken);
						return;
					}

					dialog = new InputDialog();
					await dialog.ShowDialog(_owner);

					if (dialog.Result is uint value)
						completion.TrySetResult(value);
					else
						// Dismissed without a value: treat it as a request to stop.
						completion.TrySetCanceled(cancellationToken);
				}
				catch (Exception ex)
				{
					completion.TrySetException(ex);
				}
			});

			// Stop must be able to unblock a CPU parked here, which means closing the dialog.
			using CancellationTokenRegistration registration = cancellationToken.Register(
				() => Dispatcher.UIThread.Post(() => dialog?.Close()));

			return completion.Task.GetAwaiter().GetResult();
		}
	}
}
