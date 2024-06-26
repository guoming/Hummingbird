using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;


namespace Hummingbird.Extensions.FileSystem.Oss.StaticFile
{
    
    public static class MyStaticFileExtensions
    {
        /// <summary>
        /// Enables static file serving for the current request path
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseOssStaticFiles(this IApplicationBuilder app)
        {
            return app != null ? app.UseMiddleware<OssStaticFileMiddleware>() : throw new ArgumentNullException(nameof (app));
        }

        /// <summary>
        /// Enables static file serving for the given request path
        /// </summary>
        /// <param name="app"></param>
        /// <param name="requestPath">The relative request path.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseOssStaticFiles(
            this IApplicationBuilder app,
            string requestPath)
        {
            IApplicationBuilder app1 = app != null ? app : throw new ArgumentNullException(nameof (app));
            StaticFileOptions options = new StaticFileOptions();
            options.RequestPath = new PathString(requestPath);
            return app1.UseOssStaticFiles(options);
        }

        /// <summary>Enables static file serving with the given options</summary>
        /// <param name="app"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseOssStaticFiles(
            this IApplicationBuilder app,
            StaticFileOptions options)
        {
            if (app == null)
                throw new ArgumentNullException(nameof (app));
            return options != null ? app.UseMiddleware<OssStaticFileMiddleware>((object) Microsoft.Extensions.Options.Options.Create<StaticFileOptions>(options)) : throw new ArgumentNullException(nameof (options));
        }
    }
}