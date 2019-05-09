#if NETCORE
using Hummingbird.Core;
using Hummingbird.Extersions.Cacheing.StackExchange;
#endif

using Hummingbird.Extersions.DistributedLock;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtersion
    {

#if NETCORE
        public static IHummingbirdHostBuilder AddDistributedLock(this IHummingbirdHostBuilder hostBuilder, Action<Config> action)
        {
            action = action ?? throw new ArgumentNullException(nameof(action));
            
            var option = new Config();
            action(option);

            var RedisDistributedLock = DistributedLockFactory.CreateRedisDistributedLock(option);
            hostBuilder.Services.AddSingleton<IDistributedLock>(RedisDistributedLock);
            return hostBuilder;

        }
#endif
    }
}

namespace Hummingbird.Extersions.DistributedLock
{
    public static class DistributedLockFactory
    {

        public static IDistributedLock CreateRedisDistributedLock(Config config)
        {
            return new RedisDistributedLock(Cacheing.CacheFactory.Build(option =>
            {

                option.WithDb(config.DBNum);
                option.WithKeyPrefix(config.KeyPrefix);
                option.WithWriteServerList(config.WriteServerList);
                option.WithReadServerList(config.WriteServerList);
                option.WithPassword(config.Password);
                option.WithSsl(config.Ssl);

            }));
        }
    }
}