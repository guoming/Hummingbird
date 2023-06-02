using Consul;
using Hummingbird.Core;
using Hummingbird.DynamicRoute;
using Hummingbird.Extensions.DynamicRoute.Consul;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public class ConsulConfigBuilder
    {
        private ConsulConfig _config;

        public ConsulConfigBuilder()
        {
            _config = new ConsulConfig();
        }
        public ConsulConfigBuilder(ConsulConfig config)
        {
            _config = config;
        }
    

        public void AddTags(string Tag)
        {
            if (!string.IsNullOrEmpty(Tag))
            {
                _config.SERVICE_TAGS += $",{Tag}";
            }
        }

        public ConsulConfig Build()
        {
            return _config;
        }
    }

    public static partial class DependencyInjectionExtersion
    {
        public static IServiceCollection AddConsulDynamicRoute(this IServiceCollection services, ConsulConfig config, Action<ConsulConfigBuilder> setup=null)
        {
            ConsulConfigBuilder builder = new ConsulConfigBuilder(config);

            if (setup != null)
            {
                setup(builder);
            }

            config = builder.Build();

            services.AddSingleton<ConsulConfig>(a =>
            {
                return config;
            });
            
            services.AddSingleton<IConsulClient>(a =>
            {
                
                return  new ConsulClient(delegate (ConsulClientConfiguration obj)
                {
                    obj.Address = new Uri("http://" + config.SERVICE_REGISTRY_ADDRESS + ":" + config.SERVICE_REGISTRY_PORT);
                    obj.Datacenter = config.SERVICE_REGION;
                    obj.Token = config.SERVICE_REGISTRY_TOKEN;
                });
                
            });
            services.AddSingleton<IServiceLocator>(a =>
            {
                var consul=a.GetRequiredService<IConsulClient>();
                var logger=a.GetRequiredService<ILogger<ConsulServiceLocator>>();

                return new ConsulServiceLocator(logger,consul as ConsulClient);
            });
            
            #if NETCORE
            services.AddSingleton<IServiceDiscoveryProvider, ConsulServiceDiscoveryAspCoreProvider>();
            services.AddHostedService<ConsulServiceRegisterHostedService>();
            #else
            services.AddSingleton<IServiceDiscoveryProvider, ConsulServiceDiscoveryAspNetProvider>();
            #endif        
            
            return services;
        }

        public static IServiceCollection AddConsulDynamicRoute(this IServiceCollection services,IConfiguration configuration, Action<ConsulConfigBuilder> setup=null)
        {
            var config = configuration.Get<ConsulConfig>();
            return services.AddConsulDynamicRoute(config, setup);
        }


        public static IHummingbirdHostBuilder AddConsulDynamicRoute(this IHummingbirdHostBuilder hostBuilder, IConfiguration configuration)
        {
            hostBuilder.Services.AddConsulDynamicRoute(configuration);
            return hostBuilder;
        }

        public static IHummingbirdHostBuilder AddConsulDynamicRoute(this IHummingbirdHostBuilder hostBuilder, IConfiguration configuration, Action<ConsulConfigBuilder> setup = null)
        {
            hostBuilder.Services.AddConsulDynamicRoute(configuration, setup);
            return hostBuilder;
        }
    }

}