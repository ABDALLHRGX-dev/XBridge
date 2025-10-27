using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using XBridge.Service.Services;

namespace XBridge.Host.ViewModels
{
    public class DeviceDetailViewModel : INotifyPropertyChanged
    {
        string _name;
        public string DeviceName { get => _name; set { _name = value; OnPropertyChanged(); } }
        string _serial;
        public string DeviceSerial { get => _serial; set { _serial = value; OnPropertyChanged(); } }
        string _opt;
        public string OptimizeVersion { get => _opt; set { _opt = value; OnPropertyChanged(); } }
        string _last;
        public string LastSeen { get => _last; set { _last = value; OnPropertyChanged(); } }
        string _micro;
        public string MicrobenchmarkJson { get => _micro; set { _micro = value; OnPropertyChanged(); } }

        public void LoadFromSession(DeviceSession s)
        {
            if (s == null) return;
            DeviceName = s.DeviceName;
            DeviceSerial = s.DeviceSerial;
            OptimizeVersion = s.OptimizeVersion;
            LastSeen = s.LastSeen.ToString("u");
            MicrobenchmarkJson = s.MicrobenchmarkResults == null ? string.Empty : JsonSerializer.Serialize(s.MicrobenchmarkResults, new JsonSerializerOptions { WriteIndented = true });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
