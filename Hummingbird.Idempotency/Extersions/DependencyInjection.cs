using Hummingbird.Cache;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Idempotency
{
    public static class DependencyInjectionExtersion
    {
        /// <summary>
        /// 缓存实现幂等
        /// </summary>
        /// <param name="services"></param>
        public static void AddIdempotency(this IServiceCollection services, Action<IIdempotencyOption> setupOption=null)
        {
            var option = new IdempotencyOption();

            if (setupOption != null)
            {
                setupOption(option);
            }

            services.AddSingleton<IIdempotencyOption>(option);
            services.AddSingleton<IRequestManager,CacheRequestManager>();            
        }
    }
}
