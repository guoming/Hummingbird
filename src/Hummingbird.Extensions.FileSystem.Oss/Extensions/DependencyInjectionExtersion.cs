
using System;
using Hummingbird.Core;
using Microsoft.Extensions.FileProviders;
using Hummingbird.Extensions.FileSystem;
using Hummingbird.Extensions.FileSystem.Oss;
using Hummingbird.Extensions.FileSystem.Oss.StaticFile;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using ZT.RFS.Infrastructure.FileProvider;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DependencyInjectionExtersion
    {
        public static IHummingbirdHostBuilder AddOssFileSystem(this IHummingbirdHostBuilder hostBuilder,
            IConfiguration configurationSection)
        {
            var config = configurationSection.Get<Config>();
            return AddOssFileSystem(hostBuilder, config);
        }
           
        public static IHummingbirdHostBuilder AddOssFileSystem(this IHummingbirdHostBuilder hostBuilder,
            Action<Config> configure)
        {
            var config = new Config();
            configure(config);

            return AddOssFileSystem(hostBuilder, config);
        }

        
        public static IHummingbirdHostBuilder AddOssFileSystem(this IHummingbirdHostBuilder hostBuilder, Config config)
        {
            
            hostBuilder.Services.AddSingleton<OssFileMetaInfoCacheManager>(sp =>
            {
                return new OssFileMetaInfoCacheManager(config);
            });
            
    
            hostBuilder.Services.AddSingleton<OssFileProviderManager>(sp =>
            {
                var cacheManager = sp.GetRequiredService<OssFileMetaInfoCacheManager>();
                var physicalFileProvider = new PhysicalFileProvider(config.CacheLocalPath);
                return new OssFileProviderManager(physicalFileProvider,config,cacheManager);
            });
            
            
            hostBuilder.Services.AddSingleton<IFileSystemBucket>(sp =>
            {
                var cacheManager = sp.GetRequiredService<OssFileMetaInfoCacheManager>();
                return new OssFileSystemBucket(cacheManager,config.Endpoints[config.EndpointName]);
            });

            hostBuilder.Services.AddSingleton<IFileProvider>(sp =>
            {
                var ossFileProviderManager = sp.GetRequiredService<OssFileProviderManager>();
                return ossFileProviderManager.GetFileProvider(config.EndpointName);
            });
    
            hostBuilder.Services.AddSingleton<IContentTypeProvider>(sp =>
            {
                var provider = new FileExtensionContentTypeProvider();
                provider.Mappings.Add(".exe", "application/vnd.microsoft.portable-executable");
                provider.Mappings.Add(".zpl", "application/octet-stream");
                return provider;
            });
            
            return hostBuilder;

        }
    }
}
