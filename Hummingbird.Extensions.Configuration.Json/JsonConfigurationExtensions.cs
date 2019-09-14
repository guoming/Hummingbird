using Hummingbird.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class JsonConfigurationExtensions
    {
        public static IConfigurationBuilder AddJsonFileEx(this IConfigurationBuilder builder, string path)
        {
            return AddJsonFileEx(builder, provider: builder.GetFileProvider(), path: path, optional: false, reloadOnChange: false);
        }

        public static IConfigurationBuilder AddJsonFileEx(this IConfigurationBuilder builder, string path, bool optional)
        {
            return AddJsonFileEx(builder, provider: builder.GetFileProvider(), path: path, optional: optional, reloadOnChange: false);
        }

        public static IConfigurationBuilder AddJsonFileEx(this IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange)
        {
            return AddJsonFileEx(builder, provider: builder.GetFileProvider(), path: path, optional: optional, reloadOnChange: reloadOnChange);
        }

        public static IConfigurationBuilder AddJsonFileEx(this IConfigurationBuilder builder, IFileProvider provider, string path, bool optional, bool reloadOnChange)
        {
           
            Check.NotNull(builder, "builder");
            Check.CheckCondition(() => string.IsNullOrEmpty(path), "path");
            if (provider == null && Path.IsPathRooted(path))
            {
                provider = new PhysicalFileProvider(System.IO.Directory.GetCurrentDirectory());
                path = Path.GetFileName(path);
            }
            var source = new Hummingbird.Extensions.Configuration.Json.JsonConfigurationSource
            {
                FileProvider = provider,
                Path = path,
                Optional = optional,
                ReloadOnChange = reloadOnChange
            };
            builder.Add(source);
            return builder;
        }
    }
}
