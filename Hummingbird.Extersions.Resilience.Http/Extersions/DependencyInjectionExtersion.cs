using Hummingbird.Core;
using Hummingbird.Extersions.Resilience.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtersion
    {
        public static IHummingbirdHostBuilder AddResilientHttpClient(this IHummingbirdHostBuilder hostBuilder,Action<ResilientHttpClientConfigOption> setupConfig=null)
        {
            hostBuilder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            hostBuilder.Services.AddSingleton<IHttpClientFactory, ResilientHttpClientFactory>(sp =>
            {
                var Configuration = sp.GetRequiredService<IConfiguration>();
                var logger = sp.GetRequiredService<ILogger<ResilientHttpClient>>();
                var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                var option = new ResilientHttpClientConfigOption()
                {
                     TimeoutMillseconds=1000,
                     RetryCount=3,
                     DurationSecondsOfBreak=15,
                     ExceptionsAllowedBeforeBreaking=10
                };

                if (setupConfig != null)
                {
                    setupConfig(option);
                }

                return new ResilientHttpClientFactory(logger,
                    httpContextAccessor,
                    option.TimeoutMillseconds,
                    option.ExceptionsAllowedBeforeBreaking,
                    option.RetryCount,
                    option.DurationSecondsOfBreak);
            });
            hostBuilder.Services.AddSingleton<IHttpClient>(sp => sp.GetService<IHttpClientFactory>().CreateResilientHttpClient());
            return hostBuilder;

        }

        public static IHummingbirdHostBuilder AddStandardHttpClient(this IHummingbirdHostBuilder hostBuilder)
        {
            hostBuilder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            hostBuilder.Services.AddSingleton<IHttpClientFactory, StandardHttpClientFactory>(sp =>
            {
                var Configuration = sp.GetRequiredService<IConfiguration>();
                var logger = sp.GetRequiredService<ILogger<StandardHttpClient>>();
                var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
              

                return new StandardHttpClientFactory(logger,httpContextAccessor);
            });
            hostBuilder.Services.AddSingleton<IHttpClient>(sp => sp.GetService<IHttpClientFactory>().CreateResilientHttpClient());
            return hostBuilder;

        }
    }
}
