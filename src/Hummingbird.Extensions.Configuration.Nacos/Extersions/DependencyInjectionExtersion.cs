using System;
using Hummingbird.Core;
using Hummingbird.Extensions.Configuration.Nacos;
using Microsoft.Extensions.Configuration;
using Nacos.V2.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DependencyInjectionExtersion
    {
        public static IConfigurationBuilder AddNacosConfiguration(this IConfigurationBuilder builder,NacosConfig config)
        {
            builder.AddNacosV2Configuration(a =>
            {
                a.Namespace = config.Namespace;
                a.Password = config.Password;
                a.UserName = config.UserName;
                a.AccessKey = config.AccessKey;
                a.ContextPath = config.ContextPath;
                a.EndPoint = config.EndPoint;
                a.ListenInterval = config.ListenInterval;
                a.SecretKey = config.SecretKey;
                a.ServerAddresses = config.ServerAddresses;
                a.ConfigFilterAssemblies = config.ConfigFilterAssemblies;
                a.ConfigUseRpc = config.ConfigUseRpc;
                a.DefaultTimeOut = config.DefaultTimeOut;
                a.NamingUseRpc = config.NamingUseRpc;
                a.RamRoleName = config.RamRoleName;
                a.ConfigFilterExtInfo = config.ConfigFilterExtInfo;
                a.NamingCacheRegistryDir = config.NamingCacheRegistryDir;
                a.NamingLoadCacheAtStart = config.NamingLoadCacheAtStart;
                a.NamingLoadCacheAtStart = config.NamingLoadCacheAtStart;


            });
            return builder;
        }
        
        public static IConfigurationBuilder AddNacosConfiguration(this IConfigurationBuilder builder,Action<NacosConfig> configure)
        {
            NacosConfig config =new NacosConfig();
            
            if (configure != null)
            {
                configure(config);
            }
            
            return builder.AddNacosConfiguration(config);
        }
        
        public static IConfigurationBuilder AddNacosConfiguration(this IConfigurationBuilder builder, IConfiguration configuration)
        {
            return builder.AddNacosV2Configuration(configuration);
        }
    }
}