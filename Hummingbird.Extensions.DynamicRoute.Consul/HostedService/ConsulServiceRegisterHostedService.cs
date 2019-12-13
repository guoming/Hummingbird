using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hummingbird.Extensions.DynamicRoute.Consul
{

    /// <summary>
    /// 轨迹数据采集后台服务
    /// </summary>
    public class ConsulServiceRegisterHostedService : Microsoft.Extensions.Hosting.IHostedService
    {
        private readonly ConsulConfig _serviceConfig;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IApplicationLifetime _lifetime;
        private readonly IServiceProvider _serviceProvider;

        public ConsulServiceRegisterHostedService(
            IApplicationLifetime lifetime,
            IServiceProvider serviceProvider,
            ConsulConfig serviceConfig)
        {
            _lifetime = lifetime;
            _serviceProvider = serviceProvider;
            _cancellationTokenSource = new CancellationTokenSource();            
            _serviceConfig = serviceConfig;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
           ConsulGlobalServiceRegistry.Build(_serviceProvider, a => a.WithConfig(_serviceConfig));

            _lifetime.ApplicationStarted.Register(delegate
            {
               ConsulGlobalServiceRegistry.Register();

            });
            _lifetime.ApplicationStopping.Register(delegate
            {
                ConsulGlobalServiceRegistry.Deregister();
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();
         
            return Task.CompletedTask;
        }
    }

}
