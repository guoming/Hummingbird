using Microsoft.Extensions.Configuration;
using System;
using Consul;
using Hummingbird.Core;
using Hummingbird.Extensions.DistributedLock.Consul;

namespace Microsoft.Extensions.DependencyInjection
{

    public static class DependencyInjectionExtersion
    {
        public static IHummingbirdHostBuilder AddConsulDistributedLock(this IHummingbirdHostBuilder hostBuilder, IConfiguration configuration, string AppId)
        {
            
            var config = configuration.Get<ConsulConfig>();
            hostBuilder.Services.AddSingleton<IConsulClient>(a =>
            {
                var _client = new ConsulClient(delegate (ConsulClientConfiguration obj)
                {
                    obj.Address = new Uri("http://" + config.SERVICE_REGISTRY_ADDRESS + ":" + config.SERVICE_REGISTRY_PORT);
                    obj.Datacenter = config.SERVICE_REGION;
                    obj.Token = config.SERVICE_REGISTRY_TOKEN;
                });

                return _client;

            });
            hostBuilder.Services.AddSingleton<Hummingbird.Extensions.DistributedLock.IDistributedLock>(a =>
            {
               var _client=a.GetService<IConsulClient>();

               return new ConsulDistributedLock(_client, AppId);

            });
            return hostBuilder;
        }

    }

}
