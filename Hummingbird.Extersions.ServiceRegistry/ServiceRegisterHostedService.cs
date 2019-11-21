using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hummingbird.Extersions.ServiceRegistry
{

    /// <summary>
    /// 轨迹数据采集后台服务
    /// </summary>
    public class ServiceRegisterHostedService : Microsoft.Extensions.Hosting.IHostedService
    {
        private readonly ServiceConfig _serviceConfig;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IApplicationLifetime _lifetime;
        private readonly IServiceProvider _serviceProvider;

        public ServiceRegisterHostedService(
            IApplicationLifetime lifetime,
            IServiceProvider serviceProvider,
            Hummingbird.Extersions.ServiceRegistry.ServiceConfig serviceConfig)
        {
            _lifetime = lifetime;
            _serviceProvider = serviceProvider;
            _cancellationTokenSource = new CancellationTokenSource();            
            _serviceConfig = serviceConfig;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Hummingbird.Extersions.ServiceRegistry.GlobalServiceRegistry.Build(_serviceProvider, a => a.WithConfig(_serviceConfig));

            _lifetime.ApplicationStarted.Register(delegate
            {
                Hummingbird.Extersions.ServiceRegistry.GlobalServiceRegistry.Register();

            });
            _lifetime.ApplicationStopping.Register(delegate
            {
                Hummingbird.Extersions.ServiceRegistry.GlobalServiceRegistry.Deregister();
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();
         
            return Task.CompletedTask;
        }
    }

}
