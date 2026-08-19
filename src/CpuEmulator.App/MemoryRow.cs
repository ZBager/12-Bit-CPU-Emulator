using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CpuEmulator.App
{
	/// <summary>
	/// One row of the RAM or register grid.
	/// <para>
	/// The WPF version rebuilt all 4,112 row objects every 10 ms because
	/// <see cref="Data12Bit"/> is a value type and copies do not track later writes.
	/// This mutates in place instead and only raises change notifications when the
	/// value actually moved, so a mostly-idle CPU costs almost nothing to display.
	/// </para>
	/// </summary>
	public sealed class MemoryRow : INotifyPropertyChanged
	{
		private uint _value;

		public MemoryRow(uint address)
		{
			Address = "0x" + address.ToString("X3");
		}

		/// <summary>Fixed for the lifetime of the row, so it needs no change notification.</summary>
		public string Address { get; }

		public string Hex => "0x" + _value.ToString("X3");

		public string Binary => "0b" + Convert.ToString(_value, 2).PadLeft(12, '0');

		/// <summary>Pushes a new value in, notifying only if it differs from the last one.</summary>
		public void Update(uint value)
		{
			if (_value == value)
				return;

			_value = value;
			OnPropertyChanged(nameof(Hex));
			OnPropertyChanged(nameof(Binary));
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		private void OnPropertyChanged([CallerMemberName] string? name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}
