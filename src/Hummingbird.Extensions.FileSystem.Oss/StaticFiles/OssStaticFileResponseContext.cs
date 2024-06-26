using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace Hummingbird.Extensions.FileSystem.Oss.StaticFile
{

    public class OssStaticFileResponseContext: StaticFileResponseContext
    {
        /// <summary>The request and response information.</summary>
        public HttpContext Context { get; internal set; }
    
        /// <summary>The file to be served.</summary>
        public IFileInfo File { get; internal set; }
    }
}