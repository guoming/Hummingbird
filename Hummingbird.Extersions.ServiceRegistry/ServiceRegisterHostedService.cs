using Microsoft.Extensions.Configuration;
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
        private readonly ILogger _logger;
        private readonly ServiceConfig serviceConfig;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly IApplicationLifetime _applicationLifetime;

        public ServiceRegisterHostedService(
            IServiceProvider serviceProvider,
            IApplicationLifetime applicationLifetime,
            IConfiguration configuration,
            ILogger<ServiceRegisterHostedService> logger,
            Hummingbird.Extersions.ServiceRegistry.ServiceConfig serviceConfig)
        {
            
            _serviceProvider = serviceProvider;
            _applicationLifetime = applicationLifetime;
            _configuration = configuration;
            _cancellationTokenSource = new CancellationTokenSource();            
            _logger = logger;
            this.serviceConfig = serviceConfig;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Hummingbird.Extersions.ServiceRegistry.ServiceRegistryBootstraper.Register(_serviceProvider, a => a.WithConfig(serviceConfig));

            
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();

            return Task.CompletedTask;
        }
    }

}
