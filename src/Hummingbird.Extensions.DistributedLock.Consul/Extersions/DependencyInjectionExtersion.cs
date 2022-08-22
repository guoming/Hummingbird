using Microsoft.Extensions.Configuration;
using System;
using Consul;
using Hummingbird.Core;
using Hummingbird.Extensions.DistributedLock.Consul;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection
{

    public static class DependencyInjectionExtersion
    {
        public static IHummingbirdHostBuilder AddConsulDistributedLock(this IHummingbirdHostBuilder hostBuilder, IConfiguration configuration)
        {
            var config = configuration.Get<Config>();
            return AddConsulDistributedLock(hostBuilder, config);
        }
        
        public static IHummingbirdHostBuilder AddConsulDistributedLock(this IHummingbirdHostBuilder hostBuilder, Action<Config> configuration)
        {
            configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            var config = new Config();
            configuration(config);

            return AddConsulDistributedLock(hostBuilder,config);
        }

        private static IHummingbirdHostBuilder AddConsulDistributedLock(this IHummingbirdHostBuilder hostBuilder, Config config)
        {
            if (!config.Enable.HasValue || config.Enable.Value)
            {
                hostBuilder.Services.AddSingleton<IConsulClient>(a =>
                {
                    var client = new ConsulClient(delegate(ConsulClientConfiguration obj)
                    {
                        obj.Address = new Uri("http://" + config.SERVICE_REGISTRY_ADDRESS + ":" +
                                              config.SERVICE_REGISTRY_PORT);
                        obj.Datacenter = config.SERVICE_REGION;
                        obj.Token = config.SERVICE_REGISTRY_TOKEN;
                    });

                    return client;

                });
                hostBuilder.Services.AddSingleton<Hummingbird.Extensions.DistributedLock.IDistributedLock>(sp =>
                {
                    var client = sp.GetService<IConsulClient>();
                    var logger = sp.GetRequiredService<ILogger<ConsulDistributedLock>>();
                    return new ConsulDistributedLock(client, logger,config.SERVICE_NAME);

                });
            }

            return hostBuilder;
        }



    }

}
