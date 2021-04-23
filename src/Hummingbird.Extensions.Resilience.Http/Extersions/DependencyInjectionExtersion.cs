using Hummingbird.Core;
using Hummingbird.Extensions.Resilience.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtersion
    {
        public static IHummingbirdHostBuilder AddResilientHttpClient(this IHummingbirdHostBuilder hostBuilder, Action<string,ResilientHttpClientConfigOption> func=null)
        {
            hostBuilder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            hostBuilder.Services.AddSingleton<IHttpClientFactory, ResilientHttpClientFactory>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ResilientHttpClient>>();
                var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                var serviceLocator = sp.GetService<Hummingbird.DynamicRoute.IServiceLocator>();

                return new ResilientHttpClientFactory(logger,
                    httpContextAccessor,
                    serviceLocator,
                    func);
            });
            hostBuilder.Services.AddSingleton<IHttpClient>(sp => sp.GetService<IHttpClientFactory>().CreateResilientHttpClient());
            return hostBuilder;

        }

        public static IHummingbirdHostBuilder AddResilientHttpClient(this IHummingbirdHostBuilder hostBuilder, Action<string, ResilientHttpClientConfigOption> func = null,HttpMessageHandler httpMessageHandler=null)
        {
            hostBuilder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            hostBuilder.Services.AddSingleton<IHttpClientFactory, ResilientHttpClientFactory>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ResilientHttpClient>>();
                var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                var serviceLocator = sp.GetService<Hummingbird.DynamicRoute.IServiceLocator>();

                return new ResilientHttpClientFactory(logger,
                    httpContextAccessor,
                    serviceLocator,
                    func);
            });
            hostBuilder.Services.AddSingleton<IHttpClient>(sp => sp.GetService<IHttpClientFactory>().CreateResilientHttpClient(httpMessageHandler));
            return hostBuilder;

        }

        public static IHummingbirdHostBuilder AddStandardHttpClient(this IHummingbirdHostBuilder hostBuilder)
        {
            hostBuilder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            hostBuilder.Services.AddSingleton<IHttpClientFactory, StandardHttpClientFactory>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<StandardHttpClient>>();
                var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                var serviceLocator = sp.GetService<Hummingbird.DynamicRoute.IServiceLocator>();
                return new StandardHttpClientFactory(logger,httpContextAccessor, serviceLocator);
            });
            hostBuilder.Services.AddSingleton<IHttpClient>(sp => sp.GetService<IHttpClientFactory>().CreateResilientHttpClient());
            return hostBuilder;

        }

        public static IHummingbirdHostBuilder AddStandardHttpClient(this IHummingbirdHostBuilder hostBuilder,HttpMessageHandler httpMessageHandler)
        {
            hostBuilder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            hostBuilder.Services.AddSingleton<IHttpClientFactory, StandardHttpClientFactory>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<StandardHttpClient>>();
                var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                var serviceLocator = sp.GetService<Hummingbird.DynamicRoute.IServiceLocator>();
                return new StandardHttpClientFactory(logger, httpContextAccessor, serviceLocator);
            });
            hostBuilder.Services.AddSingleton<IHttpClient>(sp => sp.GetService<IHttpClientFactory>().CreateResilientHttpClient(httpMessageHandler));
            return hostBuilder;

        }
    }
}
