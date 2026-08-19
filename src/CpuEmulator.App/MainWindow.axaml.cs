using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace CpuEmulator.App
{
	public partial class MainWindow : Window
	{
		private const int RamSize = 4096;
		private const int RegisterCount = 16;

		/// <summary>Matches the WPF version's pacing, so a running program stays watchable.</summary>
		private static readonly TimeSpan StepDelay = TimeSpan.FromMilliseconds(1);
		private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(10);

		private readonly ObservableCollection<MemoryRow> _ramRows = new();
		private readonly ObservableCollection<MemoryRow> _registerRows = new();
		private readonly DispatcherTimer _refreshTimer = new();

		private readonly Emulator _cpu;
		private CancellationTokenSource? _cancellation;
		private Task? _runTask;
		private IReadOnlyList<uint> _loadedProgram = Array.Empty<uint>();
		private string _loadedName = "none";

		private readonly TextBlock _statusText;
		private readonly TextBlock _cpuStateText;
		private readonly Button _runButton;
		private readonly Button _stopButton;
		private readonly Button _stepButton;
		private readonly Button _resetButton;

		public MainWindow()
		{
			AvaloniaXamlLoader.Load(this);

			_statusText = this.FindControl<TextBlock>("statusText")!;
			_cpuStateText = this.FindControl<TextBlock>("cpuStateText")!;
			_runButton = this.FindControl<Button>("runButton")!;
			_stopButton = this.FindControl<Button>("stopButton")!;
			_stepButton = this.FindControl<Button>("stepButton")!;
			_resetButton = this.FindControl<Button>("resetButton")!;

			_cpu = new Emulator(new UiInputSource(this));

			for (uint i = 0; i < RamSize; i++)
				_ramRows.Add(new MemoryRow(i));
			for (uint i = 0; i < RegisterCount; i++)
				_registerRows.Add(new MemoryRow(i));

			this.FindControl<DataGrid>("ramData")!.ItemsSource = _ramRows;
			this.FindControl<DataGrid>("regData")!.ItemsSource = _registerRows;

			_refreshTimer.Interval = RefreshInterval;
			_refreshTimer.Tick += (_, _) => RefreshRows();

			RefreshRows();

			if (Program.StartupProgram is string startup)
				LoadFrom(startup);
			else
				SetStatus("Load a program to begin.");
		}

		// ---- commands -------------------------------------------------------

		private async void Load_Program(object? sender, RoutedEventArgs e)
		{
			if (IsRunning)
			{
				SetStatus("Stop the CPU before loading a different program.");
				return;
			}

			try
			{
				IReadOnlyList<IStorageFolder> startIn = new List<IStorageFolder>();
				IStorageFolder? dataFolder = await StorageProvider.TryGetFolderFromPathAsync(
					Path.Combine(AppContext.BaseDirectory, "data"));

				IReadOnlyList<IStorageFile> picked = await StorageProvider.OpenFilePickerAsync(
					new FilePickerOpenOptions
					{
						Title = "Open a program listing",
						AllowMultiple = false,
						SuggestedStartLocation = dataFolder,
						FileTypeFilter = new[]
						{
							new FilePickerFileType("Program listing") { Patterns = new[] { "*.txt" } },
							FilePickerFileTypes.All,
						},
					});

				if (picked.Count == 0)
					return;

				string path = picked[0].Path.LocalPath;
				LoadFrom(path);
			}
			catch (Exception ex)
			{
				SetStatus($"Could not load the program: {ex.Message}");
			}
		}

		private void LoadFrom(string path)
		{
			try
			{
				_loadedProgram = ProgramLoader.ParseFile(path);
				_loadedName = Path.GetFileName(path);

				_cpu.Reset();
				_cpu.LoadProgram(_loadedProgram);

				RefreshRows();
				SetStatus($"Loaded {_loadedName} — {_loadedProgram.Count} words.");
			}
			catch (ProgramFormatException ex)
			{
				SetStatus($"{Path.GetFileName(path)} is not a valid listing. {ex.Message}");
			}
			catch (Exception ex)
			{
				SetStatus($"Could not load {Path.GetFileName(path)}: {ex.Message}");
			}
		}

		private async void Next_Tick(object? sender, RoutedEventArgs e)
		{
			if (IsRunning)
				return;

			if (!_cpu.IsRunning())
			{
				SetStatus("The CPU has halted. Reset to run it again.");
				return;
			}

			// Stepping happens off the UI thread because the user-input instruction
			// blocks until a dialog is answered, and that dialog needs the UI thread.
			_cancellation = new CancellationTokenSource();
			CancellationToken token = _cancellation.Token;
			SetButtons(running: true);

			try
			{
				await Task.Run(() => _cpu.NextCommand(token), token);
				SetStatus($"Stepped one instruction.");
			}
			catch (OperationCanceledException)
			{
				SetStatus("Step cancelled.");
			}
			catch (Exception ex)
			{
				ReportFault(ex);
			}
			finally
			{
				SetButtons(running: false);
				RefreshRows();
			}
		}

		private async void Start_CPU(object? sender, RoutedEventArgs e)
		{
			if (IsRunning)
				return;

			if (!_cpu.IsRunning())
			{
				SetStatus("The CPU has halted. Reset to run it again.");
				return;
			}

			_cancellation = new CancellationTokenSource();
			CancellationToken token = _cancellation.Token;

			SetButtons(running: true);
			SetStatus($"Running {_loadedName}…");
			_refreshTimer.Start();

			_runTask = Task.Run(() =>
			{
				while (_cpu.IsRunning() && !token.IsCancellationRequested)
				{
					_cpu.NextCommand(token);
					Thread.Sleep(StepDelay);
				}
			}, token);

			try
			{
				await _runTask;
				SetStatus(_cpu.IsRunning()
					? "Stopped."
					: $"{_loadedName} halted normally.");
			}
			catch (OperationCanceledException)
			{
				SetStatus("Stopped.");
			}
			catch (Exception ex)
			{
				ReportFault(ex);
			}
			finally
			{
				_refreshTimer.Stop();
				_runTask = null;
				SetButtons(running: false);
				RefreshRows();
			}
		}

		private void Stop_CPU(object? sender, RoutedEventArgs e)
		{
			// Thread.Abort() is gone from .NET, so this is cooperative: the token both
			// breaks the run loop and unblocks a CPU parked on the input dialog.
			_cancellation?.Cancel();
			SetStatus("Stopping…");
		}

		private void Reset_CPU(object? sender, RoutedEventArgs e)
		{
			if (IsRunning)
			{
				SetStatus("Stop the CPU before resetting it.");
				return;
			}

			_cpu.Reset();

			if (_loadedProgram.Count > 0)
			{
				_cpu.LoadProgram(_loadedProgram);
				SetStatus($"Reset. {_loadedName} reloaded — {_loadedProgram.Count} words.");
			}
			else
			{
				SetStatus("Reset.");
			}

			RefreshRows();
		}

		// ---- view state -----------------------------------------------------

		private bool IsRunning => _runTask is { IsCompleted: false };

		private void RefreshRows()
		{
			for (int i = 0; i < RamSize; i++)
				_ramRows[i].Update(_cpu.RAM[i].Val);

			for (int i = 0; i < RegisterCount; i++)
				_registerRows[i].Update(_cpu.REG[i].Val);

			_cpuStateText.Text =
				$"PC {_cpu.REG[15].Val:X3}   FLAGS {_cpu.DumpFlags()}   {(_cpu.IsRunning() ? "READY" : "HALTED")}";
		}

		private void SetButtons(bool running)
		{
			_runButton.IsEnabled = !running;
			_stepButton.IsEnabled = !running;
			_resetButton.IsEnabled = !running;
			_stopButton.IsEnabled = running;
		}

		private void SetStatus(string message) => _statusText.Text = message;

		private void ReportFault(Exception ex)
		{
			SetStatus(ex switch
			{
				InvalidOpcodeException opcode => opcode.Message,
				InvalidOperationException invalid => invalid.Message,
				_ => $"The CPU stopped with an error: {ex.Message}",
			});
		}
	}
}
