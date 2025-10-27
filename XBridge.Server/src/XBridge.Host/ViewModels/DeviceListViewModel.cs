using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using XBridge.Service.Services;

namespace XBridge.Host.ViewModels
{
    public class DeviceListViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<DeviceSessionViewModel> Devices { get; } = new ObservableCollection<DeviceSessionViewModel>();
        public ICommand StartListenerCommand { get; }
        public ICommand StopListenerCommand { get; }
        public ICommand ForceGlobalOptimizeCommand { get; }
        public ICommand SelectDeviceCommand { get; }

        DeviceSessionViewModel _selected;
        public DeviceSessionViewModel SelectedDevice
        {
            get => _selected;
            set { _selected = value; OnPropertyChanged(); }
        }

        readonly TcpJsonHost _host;

        public DeviceListViewModel(TcpJsonHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            StartListenerCommand = new DelegateCommand(async _ => await _host.StartAsync());
            StopListenerCommand = new DelegateCommand(async _ => await _host.StopAsync());
            ForceGlobalOptimizeCommand = new DelegateCommand(async _ => await _host.StartGlobalOptimizeAsync());
            SelectDeviceCommand = new DelegateCommand(obj => SelectedDevice = obj as DeviceSessionViewModel);
            _host.DeviceConnected += d => { Devices.Add(new DeviceSessionViewModel(d)); };
            _host.DeviceDisconnected += serial =>
            {
                var ex = Devices.FirstOrDefault(x => x.DeviceSerial == serial);
                if (ex != null) Devices.Remove(ex);
            };
            _host.DeviceUpdated += d =>
            {
                var vm = Devices.FirstOrDefault(x => x.DeviceSerial == d.DeviceSerial);
                if (vm != null) vm.Update(d);
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class DeviceSessionViewModel : INotifyPropertyChanged
    {
        public string DeviceSerial { get; private set; }
        public string DeviceName { get; private set; }
        double _share;
        public double AssignedSharePercent { get => _share; private set { _share = value; OnPropertyChanged(); } }
        public string OptimizeVersion { get; private set; }
        public string LastSeen { get; private set; }
        object _micro;
        public object MicrobenchmarkResults { get => _micro; private set { _micro = value; OnPropertyChanged(); } }

        public DeviceSessionViewModel(DeviceSession s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            DeviceSerial = s.DeviceSerial;
            DeviceName = s.DeviceName;
            AssignedSharePercent = s.AssignedSharePercent;
            OptimizeVersion = s.OptimizeVersion;
            LastSeen = s.LastSeen.ToString("u");
            MicrobenchmarkResults = s.MicrobenchmarkResults;
        }

        public void Update(DeviceSession s)
        {
            AssignedSharePercent = s.AssignedSharePercent;
            OptimizeVersion = s.OptimizeVersion;
            LastSeen = s.LastSeen.ToString("u");
            MicrobenchmarkResults = s.MicrobenchmarkResults;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class DelegateCommand : ICommand
    {
        readonly Action<object> _execute;
        readonly Func<object, bool> _can;
        public DelegateCommand(Action<object> execute, Func<object, bool> can = null) { _execute = execute; _can = can; }
        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter) => _can == null || _can(parameter);
        public void Execute(object parameter) => _execute(parameter);
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
