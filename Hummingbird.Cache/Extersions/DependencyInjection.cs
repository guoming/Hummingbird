using CacheManager.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;


namespace Hummingbird.Cache
{
    public static class DependencyInjectionExtersion
    {
        public static void AddCache(this IServiceCollection services, Action<IHummingbirdCacheOption> setupOption = null)
        {
            var config = new HummingbirdCacheOption();
            if (setupOption != null)
            {
                setupOption(config);
            }
            services.AddSingleton(typeof(IHummingbirdCacheOption), config);
            services.AddSingleton(typeof(ICacheManager<object>), sp =>
            {
                var Configuration = sp.GetRequiredService<IConfiguration>();
                var cacheConfiguration = Configuration.GetCacheConfiguration(config.configName).Builder.Build();
                var cacheManager = CacheFactory.FromConfiguration<object>(config.configName, cacheConfiguration);



                return cacheManager;
            });
            services.AddSingleton(typeof(IHummingbirdCache<object>), typeof(HummingbirdCacheManagerCache<object>));
            services.AddSingleton(typeof(IHummingbirdCache<>), typeof(HummingbirdCacheManagerCache<>));
        }
    }
}
