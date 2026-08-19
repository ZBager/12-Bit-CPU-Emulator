using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace CpuEmulator.App
{
	/// <summary>
	/// Replaces the Console.ReadLine() prompt the core used to do itself. Validation
	/// and re-prompting live here, which is why <see cref="IInputSource"/> can promise
	/// the CPU a usable value.
	/// </summary>
	public partial class InputDialog : Window
	{
		private readonly TextBox _valueBox;
		private readonly TextBlock _errorText;

		public InputDialog()
		{
			AvaloniaXamlLoader.Load(this);

			_valueBox = this.FindControl<TextBox>("valueBox")!;
			_errorText = this.FindControl<TextBlock>("errorText")!;
			this.FindControl<Button>("okButton")!.Click += OnAccept;

			Opened += (_, _) => _valueBox.Focus();
		}

		/// <summary>The accepted value, or null if the window was closed without one.</summary>
		public uint? Result { get; private set; }

		private void OnAccept(object? sender, RoutedEventArgs e)
		{
			string text = (_valueBox.Text ?? string.Empty).Trim();

			if (!uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint value))
			{
				ShowError($"'{text}' is not a hexadecimal number.");
				return;
			}

			if (value > 0xfff)
			{
				ShowError($"0x{value:X} does not fit in 12 bits. The largest value is FFF.");
				return;
			}

			Result = value;
			Close();
		}

		private void ShowError(string message)
		{
			_errorText.Text = message;
			_errorText.IsVisible = true;
			_valueBox.SelectAll();
			_valueBox.Focus();
		}
	}
}
