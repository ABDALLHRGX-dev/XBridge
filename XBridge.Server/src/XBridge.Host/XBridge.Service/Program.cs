using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using XBridge.Service.Services;
using XBridge.Service.Persistence;

namespace XBridge.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((ctx, services) =>
                {
                    services.AddSingleton<TcpJsonHost>();
                    services.AddSingleton<OptimizeStore>();
                    services.AddSingleton<SessionManager>();
                    services.AddHostedService(provider => new TcpJsonHostedService(provider.GetRequiredService<TcpJsonHost>()));
                })
                .Build();
            host.Run();
        }
    }
}
