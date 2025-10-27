using Microsoft.Extensions.DependencyInjection;
using XBridge.Service.Services;
using XBridge.Service.Persistence;

namespace XBridge.Service
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<TcpJsonHost>();
            services.AddSingleton<OptimizeStore>();
            services.AddSingleton<SessionManager>();
            services.AddHostedService(provider => new TcpJsonHostedService(provider.GetRequiredService<TcpJsonHost>()));
        }
    }
}
