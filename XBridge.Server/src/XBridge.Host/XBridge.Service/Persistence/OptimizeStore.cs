using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace XBridge.Service.Persistence
{
    public class OptimizeStore
    {
        readonly string _path;
        public OptimizeStore()
        {
            _path = Path.Combine(Directory.GetCurrentDirectory(), "optimize_store");
            if (!Directory.Exists(_path)) Directory.CreateDirectory(_path);
        }

        public Task SaveAsync(string deviceSerial, object dto)
        {
            var file = Path.Combine(_path, $"optimize_{deviceSerial}.json");
            var txt = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
            return File.WriteAllTextAsync(file, txt);
        }

        public T Load<T>(string deviceSerial) where T : class
        {
            var file = Path.Combine(_path, $"optimize_{deviceSerial}.json");
            if (!File.Exists(file)) return null;
            var txt = File.ReadAllText(file);
            return JsonSerializer.Deserialize<T>(txt);
        }

        public object Load(string deviceSerial)
        {
            var file = Path.Combine(_path, $"optimize_{deviceSerial}.json");
            if (!File.Exists(file)) return null;
            var txt = File.ReadAllText(file);
            return JsonSerializer.Deserialize<object>(txt);
        }
    }
}
