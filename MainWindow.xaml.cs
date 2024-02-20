using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Threading;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace CpuEmulator
{
	public partial class MainWindow : Window
	{
		private bool firstBoot = true;
		Emulator emulator = new Emulator();
		Thread t;
		ObservableCollection<DataGridClass> custdata1 = new ObservableCollection<DataGridClass>();
		ObservableCollection<DataGridClass> custdata2 = new ObservableCollection<DataGridClass>();
		public class DataGridClass
		{
			private Data12Bit _value;
			private Data12Bit _address;
			public DataGridClass(Data12Bit value, Data12Bit address)
			{
				_value = value;
				_address = address;
			}
			public string Address { get => _address.Val.ToString(); }
			public string Hex { get => _value.Val.ToString("X3"); }
			public string Binary { get => Convert.ToString(_value.Val, 2).PadLeft(12, '0'); }
		}
		
		public void EmulatorUpdate()
		{
			myTimer.Start();
			while (emulator.IsRunning())
			{
				Thread.Sleep(10);
				emulator.NextCommand();
			}
			Thread.Sleep(50);
			myTimer.Stop();
		}

		public MainWindow()
		{
			t = new Thread(new ThreadStart(EmulatorUpdate));
			InitializeComponent();
			for (uint i = 0; i < 4096; i++)
			{
				custdata1.Add(new DataGridClass(emulator.RAM[i], new Data12Bit(i)));
			}
			for (uint i = 0; i < 16; i++)
			{
				custdata2.Add(new DataGridClass(emulator.REG[i], new Data12Bit(i)));
			}
			ramData.DataContext = custdata1;
			regData.DataContext = custdata2;
			myTimer.Interval = TimeSpan.FromMilliseconds(10);
			myTimer.Tick += UpdateLabel;
		}

		async void Stop_CPU(object sender, RoutedEventArgs e)
		{
			if (emulator.IsRunning() == true)
			{
				t.Abort();
				myTimer.Stop();
				t = new Thread(new ThreadStart(EmulatorUpdate));
			}
		}

		void Start_CPU(object sender, RoutedEventArgs e)
		{
			if(emulator.IsRunning() == false | firstBoot == true)
				t.Start();
			firstBoot = false;
		}
		void Load_Program(object sender, RoutedEventArgs e)
		{
			emulator.LoadProgram(@"..\..\data\program.txt");
			
			UpdateDataGrid();
		}
		void Next_Tick(object sender, RoutedEventArgs e)
		{
			emulator.NextCommand();
			Thread.Sleep(20);
			UpdateDataGrid();
		}
		DispatcherTimer myTimer = new DispatcherTimer();
		private void UpdateLabel(object sender, EventArgs e)
		{
			UpdateDataGrid();
		}
		public void UpdateDataGrid()
		{
			for (int i = 0; i < 4096; i++)
			{
				custdata1[i] = new DataGridClass(emulator.RAM[i], new Data12Bit((uint)i));
			}
			for (int i = 0; i < 16; i++)
			{
				custdata2[i] = new DataGridClass(emulator.REG[i], new Data12Bit((uint)i));
			}
		}
	}
}