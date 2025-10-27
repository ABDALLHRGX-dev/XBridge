using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace XBridge.Service.Services
{
    public class GrpcHost : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
