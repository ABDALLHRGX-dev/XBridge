using System;
using System.Text.Json;
using System.Threading.Tasks;
using XBridge.Service.Persistence;

namespace XBridge.Service.Services
{
    public class OptimizeService
    {
        readonly OptimizeStore _store;
        readonly SessionManager _sessions;

        public OptimizeService(OptimizeStore store, SessionManager sessions)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        }

        public Task<HelloAck> HelloAsync(HelloMessage msg)
        {
            var ack = new HelloAck { server_version = "1.0.0", optimize_version = string.Empty, needs_optimize = false };
            if (!_sessions.Exists(msg.device_serial)) _sessions.Register(msg.device_serial, msg.device_name);
            var opt = _store.Load(msg.device_serial);
            if (opt != null) { ack.optimize_version = opt.OptimizeVersion; }
            return Task.FromResult(ack);
        }

        public Task<WorkResponse> ProcessWorkAsync(WorkRequest req)
        {
            var resultObj = new { status = "ok", task = req.task_id };
            var res = new WorkResponse { task_id = req.task_id, success = true, result = JsonSerializer.SerializeToUtf8Bytes(resultObj), error = string.Empty };
            return Task.FromResult(res);
        }
    }

    public class HelloAck { public string server_version; public string optimize_version; public bool needs_optimize; }
    public class WorkRequest { public string device_serial; public string task_id; public string task_type; public byte[] payload; public int timeout_ms; }
    public class WorkResponse { public string task_id; public bool success; public byte[] result; public string error; }
    public class HelloMessage { public string device_name; public string device_serial; public string game_full_version; }
}
