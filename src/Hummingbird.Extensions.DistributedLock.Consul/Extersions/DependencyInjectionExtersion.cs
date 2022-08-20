using Microsoft.Extensions.Configuration;
using System;
using Consul;
using Hummingbird.Core;
using Hummingbird.Extensions.DistributedLock.Consul;

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
                    var _client = new ConsulClient(delegate(ConsulClientConfiguration obj)
                    {
                        obj.Address = new Uri("http://" + config.SERVICE_REGISTRY_ADDRESS + ":" +
                                              config.SERVICE_REGISTRY_PORT);
                        obj.Datacenter = config.SERVICE_REGION;
                        obj.Token = config.SERVICE_REGISTRY_TOKEN;
                    });

                    return _client;

                });
                hostBuilder.Services.AddSingleton<Hummingbird.Extensions.DistributedLock.IDistributedLock>(a =>
                {
                    var _client = a.GetService<IConsulClient>();

                    return new ConsulDistributedLock(_client, config.SERVICE_NAME);

                });
            }

            return hostBuilder;
        }



    }

}
