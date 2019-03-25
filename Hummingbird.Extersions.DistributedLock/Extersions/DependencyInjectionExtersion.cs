using Hummingbird.Core;
using Hummingbird.Extersions.DistributedLock;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtersion
    {
        public static IHummingbirdHostBuilder AddDistributedLock(this IHummingbirdHostBuilder hostBuilder, Action<RedisCacheConfig> action)
        {
            action = action ?? throw new ArgumentNullException(nameof(action));

            var option = new RedisCacheConfig("", "", "", false, 0, "");
            action(option);

            var RedisDistributedLock = DistributedLockFactory.CreateRedisDistributedLock(option);
            hostBuilder.Services.AddSingleton<IDistributedLock>(RedisDistributedLock);

       
            return hostBuilder;

        }
    }
}
