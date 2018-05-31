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
        public static void AddCacheManager(this IServiceCollection services, IConfiguration Configuration, string configName = "HummingbirdCache")
        {          
            var cacheConfiguration = Configuration.GetCacheConfiguration(configName)
                .Builder.Build();
            var cacheManager = CacheFactory.FromConfiguration<object>(configName, cacheConfiguration);
            var hummingbirdCacheManager = new HummingbirdCacheManagerCache<object>(cacheManager);

            services.AddSingleton<ICacheManager<object>>(cacheManager);
            services.AddSingleton<IHummingbirdCache<object>>(hummingbirdCacheManager);
        }
    }
}
