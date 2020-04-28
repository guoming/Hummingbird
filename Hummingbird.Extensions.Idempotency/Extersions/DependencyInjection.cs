using Hummingbird.Core;
using Hummingbird.Extensions.Cache;
using Hummingbird.Extensions.Idempotency;

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtersion
    {
        /// <summary>
        /// 缓存实现幂等
        /// </summary>
        /// <param name="services"></param>
        public static IHummingbirdHostBuilder AddIdempotency(this IHummingbirdHostBuilder hostBuilder, Action<IIdempotencyOption> setupOption=null)
        {
            var option = new IdempotencyOption();

            if (setupOption != null)
            {
                setupOption(option);
            }

            hostBuilder.Services.AddSingleton<IIdempotencyOption>(option);
            hostBuilder.Services.AddSingleton<IRequestManager,CacheRequestManager>();
            return hostBuilder;
        }
    }
}
