

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;


namespace Hummingbird.Extensions.FileSystem.Oss.StaticFile
{

  /// <summary>Enables ser
  /// ving static files for a given request path</summary>
  public class OssStaticFileMiddleware
  {
    private readonly StaticFileOptions _options;
    private readonly PathString _matchUrl;
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;
    private readonly IFileProvider _fileProvider;
    private readonly IContentTypeProvider _contentTypeProvider;

    /// <summary>Creates a new instance of the OssStaticFileMiddleware.</summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="hostingEnv">The <see cref="T:Microsoft.AspNetCore.Hosting.IHostingEnvironment" /> used by this middleware.</param>
    /// <param name="options">The configuration options.</param>
    /// <param name="loggerFactory">An <see cref="T:Microsoft.Extensions.Logging.ILoggerFactory" /> instance used to create loggers.</param>
    public OssStaticFileMiddleware(
      RequestDelegate next,
      IHostingEnvironment hostingEnv,
      IOptions<StaticFileOptions> options,
      ILoggerFactory loggerFactory)
    {
      if (next == null)
        throw new ArgumentNullException(nameof(next));
      if (hostingEnv == null)
        throw new ArgumentNullException(nameof(hostingEnv));
      if (options == null)
        throw new ArgumentNullException(nameof(options));
      if (loggerFactory == null)
        throw new ArgumentNullException(nameof(loggerFactory));
      this._next = next;
      this._options = options.Value;
      this._contentTypeProvider = options.Value.ContentTypeProvider ??
                                  (IContentTypeProvider)new FileExtensionContentTypeProvider();
      this._fileProvider = this._options.FileProvider ?? Helpers.ResolveFileProvider(hostingEnv);
      this._matchUrl = this._options.RequestPath;
      this._logger = (ILogger)loggerFactory.CreateLogger<OssStaticFileMiddleware>();
    }

    /// <summary>
    /// Processes a request to determine if it matches a known file, and if so, serves it.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Invoke(HttpContext context)
    {
      OssStaticFileContext fileContext = new OssStaticFileContext(context, this._options, this._matchUrl, this._logger,
        this._fileProvider, this._contentTypeProvider);

      try
      {
        if (!fileContext.ValidateMethod())
          this._logger.LogRequestMethodNotSupported(context.Request.Method);
        else if (!fileContext.ValidatePath())
          this._logger.LogPathMismatch(fileContext.SubPath);
        else if (!fileContext.LookupContentType())
          this._logger.LogFileTypeNotSupported(fileContext.SubPath);
        else if (!fileContext.LookupFileInfo())
        {
          this._logger.LogFileNotFound(fileContext.SubPath);
        }
        else
        {
          fileContext.ComprehendRequestHeaders();
          switch (fileContext.GetPreconditionState())
          {
            case OssStaticFileContext.PreconditionState.Unspecified:
            case OssStaticFileContext.PreconditionState.ShouldProcess:
              if (fileContext.IsHeadMethod)
              {
                await fileContext.SendStatusAsync(200);
                return;
              }

              try
              {
                if (fileContext.IsRangeRequest)
                {
                  await fileContext.SendRangeAsync();
                  return;
                }

                await fileContext.SendAsync();
                this._logger.LogFileServed(fileContext.SubPath, fileContext.PhysicalPath);
                return;
              }
              catch (FileNotFoundException ex)
              {
                context.Response.Clear();
                break;
              }
            case OssStaticFileContext.PreconditionState.NotModified:
              this._logger.LogPathNotModified(fileContext.SubPath);
              await fileContext.SendStatusAsync(304);
              return;
            case OssStaticFileContext.PreconditionState.PreconditionFailed:
              this._logger.LogPreconditionFailed(fileContext.SubPath);
              await fileContext.SendStatusAsync(412);
              return;
            default:
              throw new NotImplementedException(fileContext.GetPreconditionState().ToString());
          }
        }

      }
      catch (Exception e)
      {
        throw;
      }
      finally
      {
        fileContext.Dispose();
      }

      await this._next(context);
    }
  }
}
