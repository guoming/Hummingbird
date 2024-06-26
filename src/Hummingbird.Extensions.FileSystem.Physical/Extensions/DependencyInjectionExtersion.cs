
using System;
using Hummingbird.Core;
using Microsoft.Extensions.FileProviders;
using Hummingbird.Extensions.FileSystem;
using Hummingbird.Extensions.FileSystem.Physical;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using ZT.RFS.Infrastructure.FileProvider;
using PhysicalFileProvider = Microsoft.Extensions.FileProviders.PhysicalFileProvider;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DependencyInjectionExtersion
    {
        
           public static IHummingbirdHostBuilder AddPhysicalFileSystem(this IHummingbirdHostBuilder hostBuilder,
               IConfiguration configurationSection)
           {
               var config = configurationSection.Get<Config>();
               
               
               return AddPhysicalFileSystem(hostBuilder, config);
           }
           
           public static IHummingbirdHostBuilder AddPhysicalFileSystem(this IHummingbirdHostBuilder hostBuilder,
               Action<Config> configure)
           {
               var config = new Config();
               configure(config);

               return AddPhysicalFileSystem(hostBuilder, config);
           }

           public static IHummingbirdHostBuilder AddPhysicalFileSystem(this IHummingbirdHostBuilder hostBuilder, Config config)
        {

            hostBuilder.Services
                .AddSingleton<PhysicalFileProvider>(sp =>
            {
                var physicalFileProvider = new PhysicalFileProvider(config.DataPath);
                return physicalFileProvider;
            });
            
            hostBuilder.Services
                .AddSingleton<IFileSystemBucket>(sp =>
            {
                return new PhysicalFileSystemBucket(config.DataPath);
            });

            hostBuilder.Services
                .AddSingleton<IFileProvider>(sp =>
            {
                return new PhysicalFileProvider(config.DataPath);
            });
    
            hostBuilder.Services
                .AddSingleton<IContentTypeProvider>(sp =>
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
