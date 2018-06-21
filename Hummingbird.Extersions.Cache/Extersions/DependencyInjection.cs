using CacheManager.Core;
using Hummingbird.Core;
using Hummingbird.Extersions.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;


namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtersion
    {
        public static IHummingbirdHostBuilder AddCache(this IHummingbirdHostBuilder hostBuilder, Action<IHummingbirdCacheOption> setupOption = null)
        {
            var config = new HummingbirdCacheOption();
            if (setupOption != null)
            {
                setupOption(config);
            }
            hostBuilder.Services.AddSingleton(typeof(IHummingbirdCacheOption), config);
            hostBuilder.Services.AddSingleton(typeof(ICacheManager<object>), sp =>
            {
                var Configuration = sp.GetRequiredService<IConfiguration>();
                var cacheConfiguration = Configuration.GetCacheConfiguration(config.ConfigName).Builder.Build();
                var cacheManager = CacheFactory.FromConfiguration<object>(config.ConfigName, cacheConfiguration);



                return cacheManager;
            });
            hostBuilder.Services.AddSingleton(typeof(IHummingbirdCache<object>), typeof(HummingbirdCacheManagerCache<object>));
            hostBuilder.Services.AddSingleton(typeof(IHummingbirdCache<>), typeof(HummingbirdCacheManagerCache<>));
            return hostBuilder;
        }
    }
}
