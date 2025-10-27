using System.Threading.Tasks;

namespace XBridge.Service.Services
{
    public class HealthService
    {
        public Task<bool> PingAsync() => Task.FromResult(true);
    }
}
