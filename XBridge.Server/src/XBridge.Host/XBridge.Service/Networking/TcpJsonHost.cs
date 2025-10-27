using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace XBridge.Service.Services
{
    public class TcpJsonHost
    {
        readonly int Port;
        TcpListener Listener;
        CancellationTokenSource Cts;
        readonly Dictionary<string, DeviceSession> Sessions = new Dictionary<string, DeviceSession>();
        public event Action<DeviceSession> DeviceConnected;
        public event Action<string> DeviceDisconnected;
        public event Action<DeviceSession> DeviceUpdated;

        public TcpJsonHost(int port = 50051)
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
            try { Cts.Cancel(); } catch { }
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
                                var session = new DeviceSession { DeviceSerial = hs.device_serial, DeviceName = hs.device_name, AssignedSharePercent = 100, OptimizeVersion = string.Empty, LastSeen = DateTime.UtcNow, MicrobenchmarkResults = null, Writer = stream };
                                Sessions[session.DeviceSerial] = session;
                                AdjustShares();
                                DeviceConnected?.Invoke(session);
                            }
                            else if (typ == "heartbeat")
                            {
                                var hs = JsonSerializer.Deserialize<HelloMessage>(txt);
                                if (Sessions.TryGetValue(hs.device_serial, out var s)) { s.LastSeen = DateTime.UtcNow; DeviceUpdated?.Invoke(s); }
                            }
                            else if (typ == "work_request")
                            {
                                var req = JsonSerializer.Deserialize<WorkRequest>(txt);
                                if (Sessions.TryGetValue(req.device_serial, out var s)) { s.LastSeen = DateTime.UtcNow; DeviceUpdated?.Invoke(s); }
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
                    if ((DateTime.UtcNow - kv.Value.LastSeen).TotalSeconds > 30) toRemove.Add(kv.Key);
                }
                foreach (var k in toRemove) { Sessions.Remove(k); DeviceDisconnected?.Invoke(k); AdjustShares(); }
            }
            catch { }
        }

        void AdjustShares()
        {
            var count = Math.Max(1, Sessions.Count);
            var share = 100.0 / count;
            foreach (var kv in Sessions) { kv.Value.AssignedSharePercent = share; kv.Value.LastSeen = DateTime.UtcNow; DeviceUpdated?.Invoke(kv.Value); }
        }

        public Task StartGlobalOptimizeAsync()
        {
            foreach (var kv in Sessions) { kv.Value.OptimizeVersion = "opt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"); kv.Value.MicrobenchmarkResults = new { status = "optimized" }; DeviceUpdated?.Invoke(kv.Value); }
            return Task.CompletedTask;
        }

        public Task ForceOptimizeForDeviceAsync(string serial)
        {
            if (Sessions.TryGetValue(serial, out var s)) { s.OptimizeVersion = "opt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"); s.MicrobenchmarkResults = new { status = "forced" }; DeviceUpdated?.Invoke(s); }
            return Task.CompletedTask;
        }

        public Task SendPingToDeviceAsync(string serial)
        {
            return Task.CompletedTask;
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
        public NetworkStream Writer { get; set; }
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
