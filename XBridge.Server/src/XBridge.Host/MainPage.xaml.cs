using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace XBridge.Host
{
    public sealed partial class MainPage : Page
    {
        ObservableCollection<DeviceSession> Devices = new ObservableCollection<DeviceSession>();
        TcpJsonHost Host;
        DeviceSession SelectedDevice;
        Config ConfigData;

        public MainPage()
        {
            this.InitializeComponent();
            DevicesList.ItemsSource = Devices;
            LoadConfig();
            Host = new TcpJsonHost(ConfigData.listen_port);
            Host.DeviceConnected += Host_DeviceConnected;
            Host.DeviceDisconnected += Host_DeviceDisconnected;
            Host.DeviceUpdated += Host_DeviceUpdated;
        }

        async void LoadConfig()
        {
            try
            {
                var folder = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
                var path = System.IO.Path.Combine(folder, "xbridge_config.json");
                if (File.Exists(path))
                {
                    var txt = await File.ReadAllTextAsync(path);
                    ConfigData = JsonSerializer.Deserialize<Config>(txt);
                }
                else
                {
                    ConfigData = new Config { listen_port = 50051, use_grpc = false, optimize_store_path = "C:\\XBridge\\optimize_store" };
                }
            }
            catch
            {
                ConfigData = new Config { listen_port = 50051, use_grpc = false, optimize_store_path = "C:\\XBridge\\optimize_store" };
            }
        }

        async void StartListenerBtn_Click(object sender, RoutedEventArgs e)
        {
            await Host.StartAsync();
            StartListenerBtn.IsEnabled = false;
            StopListenerBtn.IsEnabled = true;
        }

        async void StopListenerBtn_Click(object sender, RoutedEventArgs e)
        {
            await Host.StopAsync();
            StartListenerBtn.IsEnabled = true;
            StopListenerBtn.IsEnabled = false;
        }

        async void ForceGlobalOptimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            await Host.StartGlobalOptimizeAsync();
        }

        void DevicesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedDevice = DevicesList.SelectedItem as DeviceSession;
            UpdateDetails();
        }

        void UpdateDetails()
        {
            if (SelectedDevice == null)
            {
                DetailName.Text = string.Empty;
                DetailSerial.Text = string.Empty;
                DetailOptimizeVersion.Text = string.Empty;
                DetailLastSeen.Text = string.Empty;
                DetailMicrobench.Text = string.Empty;
                return;
            }
            DetailName.Text = SelectedDevice.DeviceName;
            DetailSerial.Text = SelectedDevice.DeviceSerial;
            DetailOptimizeVersion.Text = SelectedDevice.OptimizeVersion ?? string.Empty;
            DetailLastSeen.Text = SelectedDevice.LastSeen.ToString("u");
            if (SelectedDevice.MicrobenchmarkResults != null)
            {
                DetailMicrobench.Text = JsonSerializer.Serialize(SelectedDevice.MicrobenchmarkResults, new JsonSerializerOptions { WriteIndented = true });
            }
            else
            {
                DetailMicrobench.Text = string.Empty;
            }
        }

        async void ForceOptimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedDevice == null) return;
            await Host.ForceOptimizeForDeviceAsync(SelectedDevice.DeviceSerial);
        }

        async void SendPingBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedDevice == null) return;
            await Host.SendPingToDeviceAsync(SelectedDevice.DeviceSerial);
        }

        async void Host_DeviceConnected(DeviceSession obj)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Devices.Add(obj);
            });
        }

        async void Host_DeviceDisconnected(string serial)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var d = FindDevice(serial);
                if (d != null) Devices.Remove(d);
            });
        }

        async void Host_DeviceUpdated(DeviceSession obj)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var existing = FindDevice(obj.DeviceSerial);
                if (existing != null)
                {
                    existing.AssignedSharePercent = obj.AssignedSharePercent;
                    existing.LastSeen = obj.LastSeen;
                    existing.OptimizeVersion = obj.OptimizeVersion;
                    existing.MicrobenchmarkResults = obj.MicrobenchmarkResults;
                    UpdateDetails();
                    DevicesList.ItemsSource = null;
                    DevicesList.ItemsSource = Devices;
                }
            });
        }

        DeviceSession FindDevice(string serial)
        {
            foreach (var d in Devices) if (d.DeviceSerial == serial) return d;
            return null;
        }
    }

    public class DeviceSession
    {
        public string DeviceSerial { get; set; }
        public string DeviceName { get; set; }
        public double AssignedSharePercent { get; set; }
        public string OptimizeVersion { get; set; }
        public DateTime LastSeen { get; set; }
        public object MicrobenchmarkResults { get; set; }
    }

    public class Config
    {
        public int listen_port { get; set; }
        public bool use_grpc { get; set; }
        public string optimize_store_path { get; set; }
    }

    public class TcpJsonHost
    {
        readonly int Port;
        TcpListener Listener;
        CancellationTokenSource Cts;
        readonly Dictionary<string, DeviceSession> Sessions = new Dictionary<string, DeviceSession>();
        public event Action<DeviceSession> DeviceConnected;
        public event Action<string> DeviceDisconnected;
        public event Action<DeviceSession> DeviceUpdated;

        public TcpJsonHost(int port)
        {
            Port = port;
        }

        public Task StartAsync()
        {
            Cts = new CancellationTokenSource();
            Listener = new TcpListener(IPAddress.Any, Port);
            Listener.Start();
            Task.Run(() => AcceptLoop(Cts.Token));
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            try
            {
                Cts.Cancel();
            }
            catch { }
            try { Listener.Stop(); } catch { }
            return Task.CompletedTask;
        }

        async Task AcceptLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                TcpClient client = null;
                try
                {
                    client = await Listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client, ct));
                }
                catch
                {
                    client?.Close();
                }
            }
        }

        async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                var buffer = new byte[8192];
                var ms = new MemoryStream();
                int read = 0;
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        read = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
                        if (read == 0) break;
                        ms.Write(buffer, 0, read);
                        if (stream.DataAvailable) continue;
                        var txt = Encoding.UTF8.GetString(ms.ToArray());
                        ms.SetLength(0);
                        var doc = JsonDocument.Parse(txt);
                        if (doc.RootElement.TryGetProperty("type", out var t))
                        {
                            var typ = t.GetString();
                            if (typ == "hello")
                            {
                                var hs = JsonSerializer.Deserialize<HelloMessage>(txt);
                                var session = new DeviceSession
                                {
                                    DeviceSerial = hs.device_serial,
                                    DeviceName = hs.device_name,
                                    AssignedSharePercent = 100,
                                    OptimizeVersion = string.Empty,
                                    LastSeen = DateTime.UtcNow,
                                    MicrobenchmarkResults = null
                                };
                                Sessions[session.DeviceSerial] = session;
                                AdjustShares();
                                DeviceConnected?.Invoke(session);
                            }
                            else if (typ == "heartbeat")
                            {
                                var hs = JsonSerializer.Deserialize<HelloMessage>(txt);
                                if (Sessions.TryGetValue(hs.device_serial, out var s))
                                {
                                    s.LastSeen = DateTime.UtcNow;
                                    DeviceUpdated?.Invoke(s);
                                }
                            }
                            else if (typ == "work_request")
                            {
                                var req = JsonSerializer.Deserialize<WorkRequest>(txt);
                                if (Sessions.TryGetValue(req.device_serial, out var s))
                                {
                                    s.LastSeen = DateTime.UtcNow;
                                    DeviceUpdated?.Invoke(s);
                                }
                            }
                        }
                    }
                    catch
                    {
                        break;
                    }
                }
            }
            try
            {
                var toRemove = new List<string>();
                foreach (var kv in Sessions)
                {
                    if ((DateTime.UtcNow - kv.Value.LastSeen).TotalSeconds > 30)
                    {
                        toRemove.Add(kv.Key);
                    }
                }
                foreach (var k in toRemove)
                {
                    Sessions.Remove(k);
                    DeviceDisconnected?.Invoke(k);
                    AdjustShares();
                }
            }
            catch { }
        }

        void AdjustShares()
        {
            var count = Math.Max(1, Sessions.Count);
            var share = 100.0 / count;
            foreach (var kv in Sessions)
            {
                kv.Value.AssignedSharePercent = share;
                kv.Value.LastSeen = DateTime.UtcNow;
                DeviceUpdated?.Invoke(kv.Value);
            }
        }

        public Task StartGlobalOptimizeAsync()
        {
            foreach (var kv in Sessions)
            {
                kv.Value.OptimizeVersion = "opt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                kv.Value.MicrobenchmarkResults = new { status = "optimized" };
                DeviceUpdated?.Invoke(kv.Value);
            }
            return Task.CompletedTask;
        }

        public Task ForceOptimizeForDeviceAsync(string serial)
        {
            if (Sessions.TryGetValue(serial, out var s))
            {
                s.OptimizeVersion = "opt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                s.MicrobenchmarkResults = new { status = "forced" };
                DeviceUpdated?.Invoke(s);
            }
            return Task.CompletedTask;
        }

        public Task SendPingToDeviceAsync(string serial)
        {
            return Task.CompletedTask;
        }
    }

    public class HelloMessage
    {
        public string type { get; set; }
        public string device_name { get; set; }
        public string device_serial { get; set; }
        public string game_full_version { get; set; }
        public JsonElement capabilities { get; set; }
    }

    public class WorkRequest
    {
        public string type { get; set; }
        public string device_serial { get; set; }
        public string task_id { get; set; }
        public string task_type { get; set; }
        public string payload { get; set; }
        public int timeout_ms { get; set; }
    }
}
