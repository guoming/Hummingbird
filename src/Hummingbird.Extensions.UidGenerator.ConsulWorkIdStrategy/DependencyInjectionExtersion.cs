using Consul;
using Hummingbird.Extensions.UidGenerator;
using Hummingbird.Extensions.UidGenerator.Abastracts;
using Hummingbird.Extensions.UidGenerator.ConsulWorkIdStrategy;
using Hummingbird.Extensions.UidGenerator.WorkIdCreateStrategy;
using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.Extensions.DependencyInjection
{

    public static class DependencyInjectionExtersion
    {
        public static IWorkIdCreateStrategyBuilder AddConsulWorkIdCreateStrategy(this IWorkIdCreateStrategyBuilder hostBuilder, IConfiguration configuration, string AppId)
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

            hostBuilder.Services.AddSingleton<IWorkIdCreateStrategy>(sp =>
            {
                var consulClient = sp.GetService<IConsulClient>();
                var strategy = new ConsulWorkIdCreateStrategy(consulClient, AppId);
                return strategy;
            });

            return hostBuilder;
        }


        public static IWorkIdCreateStrategyBuilder AddConsulWorkIdCreateStrategy(this IWorkIdCreateStrategyBuilder hostBuilder, string AppId)
        {
            hostBuilder.Services.AddSingleton<IWorkIdCreateStrategy>(sp =>
            {
                var consulClient = sp.GetService<IConsulClient>();
                var strategy = new ConsulWorkIdCreateStrategy(consulClient, AppId);
                return strategy;
            });

            return hostBuilder;
        }
    }

}
