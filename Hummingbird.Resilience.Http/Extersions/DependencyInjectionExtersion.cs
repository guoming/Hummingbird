using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Resilience.Http
{
    public static class DependencyInjectionExtersion
    {
        public static void AddResilientHttpClient(this IServiceCollection services,Action<ResilientHttpClientConfigOption> setupConfig=null)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IHttpClientFactory, ResilientHttpClientFactory>(sp =>
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
            services.AddSingleton<IHttpClient>(sp => sp.GetService<IHttpClientFactory>().CreateResilientHttpClient());

        }

        public static void AddStandardHttpClient(this IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IHttpClientFactory, StandardHttpClientFactory>(sp =>
            {
                var Configuration = sp.GetRequiredService<IConfiguration>();
                var logger = sp.GetRequiredService<ILogger<StandardHttpClient>>();
                var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
              

                return new StandardHttpClientFactory(logger,httpContextAccessor);
            });
            services.AddSingleton<IHttpClient>(sp => sp.GetService<IHttpClientFactory>().CreateResilientHttpClient());

        }
    }
}
