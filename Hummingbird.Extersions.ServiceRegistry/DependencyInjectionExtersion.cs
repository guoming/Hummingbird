using Consul;
using Hummingbird.Core;
using Hummingbird.Extersions.ServiceRegistry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
#if NETCORE

    public static partial class DependencyInjectionExtersion
    {
        public static IServiceCollection AddServiceRegisterHostedService(this IServiceCollection services,IConfiguration configuration)
        {
            services.AddSingleton<Hummingbird.Extersions.ServiceRegistry.ServiceConfig>(a =>
            {
                return configuration.Get<Hummingbird.Extersions.ServiceRegistry.ServiceConfig>();
            });
                
            services.AddHostedService<ServiceRegisterHostedService>();
            return services;
        }

        public static IServiceCollection AddServiceRegisterHostedService(this IServiceCollection services, Action<Hummingbird.Extersions.ServiceRegistry.ServiceConfig> setup)
        {
            services.AddSingleton<Hummingbird.Extersions.ServiceRegistry.ServiceConfig>(a =>
            {
                var config = new Hummingbird.Extersions.ServiceRegistry.ServiceConfig();
                setup(config);
                return config;
            });

            services.AddHostedService<ServiceRegisterHostedService>();
            return services;
        }


        public static IHummingbirdHostBuilder AddServiceRegisterHostedService(this IHummingbirdHostBuilder hostBuilder, IConfiguration configuration)
        {
            hostBuilder.Services.AddSingleton<Hummingbird.Extersions.ServiceRegistry.ServiceConfig>(a =>
            {
                return configuration.Get<Hummingbird.Extersions.ServiceRegistry.ServiceConfig>();
            });

            hostBuilder.Services.AddHostedService<ServiceRegisterHostedService>();
            return hostBuilder;
        }


        public static IHummingbirdHostBuilder AddServiceRegisterHostedService(this IHummingbirdHostBuilder hostBuilder, Action<Hummingbird.Extersions.ServiceRegistry.ServiceConfig> setup)
        {
            hostBuilder.Services.AddSingleton<Hummingbird.Extersions.ServiceRegistry.ServiceConfig>(a =>
            {
                var config = new Hummingbird.Extersions.ServiceRegistry.ServiceConfig();
                setup(config);
                return config;
            });
            
            hostBuilder.Services.AddHostedService<ServiceRegisterHostedService>();
            return hostBuilder;
        }
    }
#endif
}