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
        public class DataGridClass
        {
            private Data12Bit _value;
            private Data12Bit _address;
            public DataGridClass(Data12Bit value, Data12Bit address)
            {
                _value = value;
                _address = address;
            }
            public string Address { get => _address.ValA.ToString(); }
            public string Hex { get => _value.ValA.ToString("X3"); }
            public string Binary { get => Convert.ToString(_value.ValA, 2).PadLeft(12, '0'); }
        }
        Emulator emulator = new Emulator();
        Thread t;
        ObservableCollection<DataGridClass> custdata1 = new ObservableCollection<DataGridClass>();
        ObservableCollection<DataGridClass> custdata2 = new ObservableCollection<DataGridClass>();
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
        public async Task ExampleMethodAsync()
        {
            johnxina.Opacity = 0;
            await Task.Delay(3000);
            johnxina.Opacity = 1;
        }
        async void Stop_CPU(object sender, RoutedEventArgs e)
        {
            t.Abort();
            myTimer.Stop();
            t = new Thread(new ThreadStart(EmulatorUpdate));
            await ExampleMethodAsync();
        }
        void Start_CPU(object sender, RoutedEventArgs e)
        {
            t.Start();
        }
        void Load_Program(object sender, RoutedEventArgs e)
        {
            emulator.LoadProgram(@"..\..\data\program.txt");
            
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