using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Hummingbird.Extensions.FileSystem.Oss.StaticFile
{ 
    
    internal static class Helpers
    {
        internal static IFileProvider ResolveFileProvider(IHostingEnvironment hostingEnv)
        {
            return hostingEnv.WebRootFileProvider != null ? hostingEnv.WebRootFileProvider : throw new InvalidOperationException("Missing FileProvider.");
        }

        internal static bool IsGetOrHeadMethod(string method)
        {
            return HttpMethods.IsGet(method) || HttpMethods.IsHead(method);
        }

        internal static bool PathEndsInSlash(PathString path)
        {
            return path.Value.EndsWith("/", StringComparison.Ordinal);
        }

        internal static bool TryMatchPath(
            HttpContext context,
            PathString matchUrl,
            bool forDirectory,
            out PathString subpath)
        {
            PathString path = context.Request.Path;
            if (forDirectory && !Helpers.PathEndsInSlash(path))
                path += new PathString("/");
            return path.StartsWithSegments(matchUrl, out subpath);
        }
    }
}