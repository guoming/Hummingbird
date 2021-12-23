using Hummingbird.DynamicRoute;
using Hummingbird.Extensions.HealthChecks;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
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
        private readonly IServiceDiscoveryProvider _serviceDiscoveryProvider;
        private readonly System.Timers.Timer _timer;
        public ConsulServiceRegisterHostedService(
            IApplicationLifetime lifetime,
            IServiceProvider serviceProvider,
            IServiceDiscoveryProvider serviceDiscoveryProvider,
            ConsulConfig serviceConfig)
        {
            _lifetime = lifetime;
            _serviceProvider = serviceProvider;
            _cancellationTokenSource = new CancellationTokenSource();            
            _serviceConfig = serviceConfig;
            _serviceDiscoveryProvider = serviceDiscoveryProvider;
            _timer = new System.Timers.Timer((double)(int.Parse(_serviceConfig.SERVICE_CHECK_INTERVAL) * 1000));

        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {

            _lifetime.ApplicationStarted.Register(delegate
            {
                _serviceDiscoveryProvider.Register();

                _timer.Elapsed +=  delegate
                {
                    _serviceDiscoveryProvider.Heartbeat();
                };
                _timer.Start();
            });
            _lifetime.ApplicationStopping.Register(delegate
            {
                _timer.Stop();
                _serviceDiscoveryProvider.Deregister();

            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();
         
            return Task.CompletedTask;
        }
    }

}
