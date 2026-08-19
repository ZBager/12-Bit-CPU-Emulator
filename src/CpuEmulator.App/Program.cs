using Avalonia;

namespace CpuEmulator.App
{
	internal static class Program
	{
		/// <summary>
		/// Optional program listing named on the command line, loaded at startup.
		/// Makes the app scriptable and saves a trip through the file picker.
		/// </summary>
		public static string? StartupProgram { get; private set; }

		[STAThread]
		public static void Main(string[] args)
		{
			StartupProgram = args.FirstOrDefault(a => !a.StartsWith('-'));
			BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
		}

		public static AppBuilder BuildAvaloniaApp()
			=> AppBuilder.Configure<App>()
				.UsePlatformDetect()
				.LogToTrace();
	}
}
