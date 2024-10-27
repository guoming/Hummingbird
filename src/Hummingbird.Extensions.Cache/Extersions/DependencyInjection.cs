﻿using System;
using Hummingbird.Extensions.Cache;
using CacheManager.Core;
using CacheManager.Redis;

#if NETCORE
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configurations;
using CacheManager.MicrosoftCachingMemory;
using Hummingbird.Core;
#else
using CacheManager.SystemRuntimeCaching;
#endif

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtersion
    {
#if NETCORE
        static ICacheManager<T> GetCacheManager<T>(IServiceProvider sp,string ConfigName)
        {
            var Configurations = sp.GetRequiredService<IConfiguration>();
            var cacheConfiguration = Configurations.GetCacheConfiguration(ConfigName).Builder.Build();
            var cacheManager = CacheManager.Core.CacheFactory.FromConfiguration<T>(ConfigName, cacheConfiguration);
            return cacheManager;
        }


        public static IHummingbirdHostBuilder AddCache(this IHummingbirdHostBuilder hostBuilder, Action<IHummingbirdCacheConfig> setupOption = null)
        {
            var config = new HummingbirdCacheConfig();
            if (setupOption != null)
            {
                setupOption(config);
            }
            hostBuilder.Services.AddSingleton(typeof(IHummingbirdCacheConfig), config);
            hostBuilder.Services.AddSingleton(typeof(ICacheManager<int>), sp =>
            {
                return GetCacheManager<int>(sp, config.ConfigName);
            });
            hostBuilder.Services.AddSingleton(typeof(ICacheManager<long>), sp =>
            {
                return GetCacheManager<int>(sp, config.ConfigName);
            });
            hostBuilder.Services.AddSingleton(typeof(ICacheManager<string>), sp =>
            {
                return GetCacheManager<string>(sp, config.ConfigName);
            });
            hostBuilder.Services.AddSingleton(typeof(ICacheManager<bool>), sp =>
            {
                return GetCacheManager<bool>(sp, config.ConfigName);
            });
            hostBuilder.Services.AddSingleton(typeof(ICacheManager<object>), sp =>
            {
                return GetCacheManager<object>(sp, config.ConfigName);
            });

            hostBuilder.Services.AddSingleton(typeof(IHummingbirdCache<int>), sp => {
                var cacheManager = sp.GetRequiredService<ICacheManager<int>>();
                return new Hummingbird.Extensions.Cache.HummingbirdCacheManagerCache<int>(cacheManager, config.CacheRegion);
            });
            hostBuilder.Services.AddSingleton(typeof(IHummingbirdCache<long>), sp => {
                var cacheManager = sp.GetRequiredService<ICacheManager<long>>();
                return new Hummingbird.Extensions.Cache.HummingbirdCacheManagerCache<long>(cacheManager, config.CacheRegion);
            });
            hostBuilder.Services.AddSingleton(typeof(IHummingbirdCache<string>), sp => {
                var cacheManager = sp.GetRequiredService<ICacheManager<string>>();
                return new Hummingbird.Extensions.Cache.HummingbirdCacheManagerCache<string>(cacheManager, config.CacheRegion);
            });
            hostBuilder.Services.AddSingleton(typeof(IHummingbirdCache<bool>), sp => {
                var cacheManager = sp.GetRequiredService<ICacheManager<bool>>();
                return new Hummingbird.Extensions.Cache.HummingbirdCacheManagerCache<bool>(cacheManager, config.CacheRegion);
            });
            hostBuilder.Services.AddSingleton(typeof(IHummingbirdCache<object>), sp => {
                var cacheManager = sp.GetRequiredService<ICacheManager<object>>();
                return new Hummingbird.Extensions.Cache.HummingbirdCacheManagerCache<object>(cacheManager, config.CacheRegion);
            });
            return hostBuilder;
        }

        public static IHummingbirdHostBuilder AddCache(this IHummingbirdHostBuilder hostBuilder, Action<Hummingbird.Extensions.Cache.RedisConfigurationBuilder> configuration, string CacheRegion = "")
        {
            hostBuilder.Services.AddSingleton(typeof(IHummingbirdCache<int>), sp =>
            {
                return Hummingbird.Extensions.Cache.CacheFactory.Build<int>(configuration, CacheRegion);
            });
            hostBuilder.Services.AddSingleton(typeof(IHummingbirdCache<long>), sp =>
            {
                return Hummingbird.Extensions.Cache.CacheFactory.Build<long>(configuration, CacheRegion);
            });
            hostBuilder.Services.AddSingleton(typeof(IHummingbirdCache<string>), sp =>
            {
                return Hummingbird.Extensions.Cache.CacheFactory.Build<string>(configuration, CacheRegion);
            });
            hostBuilder.Services.AddSingleton(typeof(IHummingbirdCache<bool>), sp =>
            {
                return Hummingbird.Extensions.Cache.CacheFactory.Build<bool>(configuration, CacheRegion);
            });
            hostBuilder.Services.AddSingleton(typeof(IHummingbirdCache<object>), sp =>
            {
                return Hummingbird.Extensions.Cache.CacheFactory.Build<object>(configuration, CacheRegion);
            });
            return hostBuilder;

        }
#endif
    }
}

namespace Hummingbird.Extensions.Cache
{
    public class RedisConfigurationBuilder
    {
        private int ConnectionTimeout { get; set; } = 0;
        private bool AllowAdmin { get; set; } = true;
        private string Password { get; set; } = "";
        private string Host { get; set; } = "localhost";
        private int Port { get; set; } = 6378;
        private int Database { get; set; } = 0;

        public bool Ssl { get; set; } = false;

        public RedisConfigurationBuilder WithAllowAdmin()
        {
            this.AllowAdmin = true;
            return this;
        }

        public RedisConfigurationBuilder WithSsl()
        {
            this.Ssl = true;
            return this;
        }

        public RedisConfigurationBuilder WithDatabase(int database)
        {
            this.Database = database;
            return this;
        }
        public RedisConfigurationBuilder WithPassword(string password)
        {
            this.Password = password;
            return this;
        }
        public RedisConfigurationBuilder WithEndpoint(string host, int port)
        {
            this.Host = host;
            this.Port = port;
            return this;
        }

        public RedisConfigurationBuilder WithConnectionTimeout(int timeout)
        {
            this.ConnectionTimeout = timeout;
            return this;
        }

        public Action<CacheManager.Redis.RedisConfigurationBuilder> Build()
        {

            return (redis) =>
            {
                if (AllowAdmin)
                {
                    redis.WithAllowAdmin();
                }

                if (Ssl)
                {
                    redis.WithSsl(this.Host);
                }

                if (ConnectionTimeout > 0)
                {
                    redis.WithConnectionTimeout(ConnectionTimeout);
                }

                redis.WithDatabase(Database)
                .WithEndpoint(Host, Port)
                .WithPassword(Password);
            };
        }

    }

    public static class CacheFactory
    {
#if NETCORE
        public static IHummingbirdCache<T> Build<T>(Action<RedisConfigurationBuilder> configuration, string CacheRegion = "")
        {
            var _builder = new RedisConfigurationBuilder();
            configuration(_builder);

            var cacheManager = CacheManager.Core.CacheFactory.Build<T>("getStartedCache", settings =>
            {
                settings.WithJsonSerializer();
                settings.WithMicrosoftMemoryCacheHandle("handleName")
                .And
                .WithRedisConfiguration("redis", _builder.Build())
                .WithMaxRetries(100)
                .WithRetryTimeout(50)
                .WithRedisBackplane("redis")
                .WithRedisCacheHandle("redis", true);
            });

            Hummingbird.Extensions.Cache.IHummingbirdCache<T> cache = new Hummingbird.Extensions.Cache.HummingbirdCacheManagerCache<T>(cacheManager, CacheRegion);
            return cache;
        }
#else
        public static IHummingbirdCache<T> Build<T>(Action<RedisConfigurationBuilder> configuration,string CacheRegion="")
        {
          
            var _builder = new RedisConfigurationBuilder();
            configuration(_builder);       

            var cacheManager = CacheManager.Core.CacheFactory.Build<T>("getStartedCache", settings =>
            {
                settings.WithJsonSerializer();
                settings.WithSystemRuntimeCacheHandle("handleName")
                .And
                .WithRedisConfiguration("redis",_builder.Build())
                .WithMaxRetries(100)
                .WithRetryTimeout(50)
                .WithRedisBackplane("redis")
                .WithRedisCacheHandle("redis", true);
            });

            Hummingbird.Extensions.Cache.IHummingbirdCache<T> cache = new Hummingbird.Extensions.Cache.HummingbirdCacheManagerCache<T>(cacheManager, CacheRegion);
            return cache;
        }    
#endif
    }
}