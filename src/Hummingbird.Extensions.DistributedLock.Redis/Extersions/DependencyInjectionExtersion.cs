
using Hummingbird.Core;


using Hummingbird.Extensions.DistributedLock;
using System;
using Hummingbird.Extensions.DistributedLock.Redis;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtersion
    {
        
        public static IHummingbirdHostBuilder AddRedisDistributedLock(this IHummingbirdHostBuilder hostBuilder, Action<Config> action)
        {
            action = action ?? throw new ArgumentNullException(nameof(action));
       
            hostBuilder.Services.AddSingleton<IDistributedLock>(sp =>
            {
                var config = new Config();
                action(config);

                return new RedisDistributedLock(Hummingbird.Extensions.Cacheing.CacheFactory.Build(option =>
                {
                    option.WithDb(config.DBNum);
                    option.WithKeyPrefix(config.KeyPrefix);
                    option.WithWriteServerList(config.WriteServerList);
                    option.WithReadServerList(config.WriteServerList);
                    option.WithPassword(config.Password);
                    option.WithSsl(config.Ssl);

                }), sp.GetService<ILogger<RedisDistributedLock>>(),TimeSpan.FromSeconds(config.LockExpirySeconds));
                
            });
            return hostBuilder;

        }

    }
}