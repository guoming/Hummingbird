using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hummingbird.Extensions.FileSystem;
using ZT.RFS.Infrastructure.FileProvider;

namespace Hummingbird.Extensions.FileSystem.Oss.StaticFile
{
  
  public class OssStaticFileContext: IDisposable
  {
    private const int StreamCopyBufferSize = 65536;
    private readonly HttpContext _context;
    private readonly StaticFileOptions _options;
    private readonly PathString _matchUrl;
    private readonly HttpRequest _request;
    private readonly HttpResponse _response;
    private readonly ILogger _logger;
    private readonly IFileProvider _fileProvider;
    private readonly Microsoft.AspNetCore.StaticFiles.IContentTypeProvider _contentTypeProvider;
    private string _method;
    private bool _isGet;
    private bool _isHead;
    private PathString _subPath;
    private string _contentType;
    private OssStaticFileInfo _fileInfo;
    private long _length;
    private DateTimeOffset _lastModified;
    private EntityTagHeaderValue _etag;
    private RequestHeaders _requestHeaders;
    private ResponseHeaders _responseHeaders;
    private OssStaticFileContext.PreconditionState _ifMatchState;
    private OssStaticFileContext.PreconditionState _ifNoneMatchState;
    private OssStaticFileContext.PreconditionState _ifModifiedSinceState;
    private OssStaticFileContext.PreconditionState _ifUnmodifiedSinceState;
    private RangeItemHeaderValue _range;
    private bool _isRangeRequest;

    public OssStaticFileContext(
      HttpContext context,
      StaticFileOptions options,
      PathString matchUrl,
      ILogger logger,
      IFileProvider fileProvider,
      Microsoft.AspNetCore.StaticFiles.IContentTypeProvider contentTypeProvider)
    {
      this._context = context;
      this._options = options;
      this._matchUrl = matchUrl;
      this._request = context.Request;
      this._response = context.Response;
      this._logger = logger;
      this._requestHeaders = this._request.GetTypedHeaders();
      this._responseHeaders = this._response.GetTypedHeaders();
      this._fileProvider = fileProvider;
      this._contentTypeProvider = contentTypeProvider;
      this._method = (string) null;
      this._isGet = false;
      this._isHead = false;
      this._subPath = PathString.Empty;
      this._contentType = (string) null;
      this._fileInfo = (OssStaticFileInfo) null;
      this._length = 0L;
      this._lastModified = new DateTimeOffset();
      this._etag = (EntityTagHeaderValue) null;
      this._ifMatchState = OssStaticFileContext.PreconditionState.Unspecified;
      this._ifNoneMatchState = OssStaticFileContext.PreconditionState.Unspecified;
      this._ifModifiedSinceState = OssStaticFileContext.PreconditionState.Unspecified;
      this._ifUnmodifiedSinceState = OssStaticFileContext.PreconditionState.Unspecified;
      this._range = (RangeItemHeaderValue) null;
      this._isRangeRequest = false;
    }

    public bool IsHeadMethod => this._isHead;

    public bool IsRangeRequest => this._isRangeRequest;

    public string SubPath => this._subPath.Value;

    public string PhysicalPath => this._fileInfo?.PhysicalPath;

    public bool ValidateMethod()
    {
      this._method = this._request.Method;
      this._isGet = HttpMethods.IsGet(this._method);
      this._isHead = HttpMethods.IsHead(this._method);
      return this._isGet || this._isHead;
    }

    public bool ValidatePath()
    {
      return Helpers.TryMatchPath(this._context, this._matchUrl, false, out this._subPath);
    }

    public bool LookupContentType()
    {
      if (this._contentTypeProvider.TryGetContentType(this._subPath.Value, out this._contentType))
        return true;
      if (!this._options.ServeUnknownFileTypes)
        return false;
      this._contentType = this._options.DefaultContentType;
      return true;
    }

    public bool LookupFileInfo()
    {
      this._fileInfo = new OssStaticFileInfo(this._fileProvider.GetFileInfo(this._subPath.Value));
      if (this._fileInfo.Exists)
      {
        this._length = this._fileInfo.Length;
        DateTimeOffset lastModified = this._fileInfo.LastModified;
        this._lastModified = new DateTimeOffset(lastModified.Year, lastModified.Month, lastModified.Day, lastModified.Hour, lastModified.Minute, lastModified.Second, lastModified.Offset).ToUniversalTime();
        this._etag = new EntityTagHeaderValue((StringSegment) ("\"" + Convert.ToString(this._lastModified.ToFileTime() ^ this._length, 16) + "\""));
      }
      return this._fileInfo.Exists;
    }

    public void ComprehendRequestHeaders()
    {
      this.ComputeIfMatch();
      this.ComputeIfModifiedSince();
      this.ComputeRange();
      this.ComputeIfRange();
    }

    private void ComputeIfMatch()
    {
      IList<EntityTagHeaderValue> ifMatch = this._requestHeaders.IfMatch;
      if (ifMatch != null && ifMatch.Any<EntityTagHeaderValue>())
      {
        this._ifMatchState = OssStaticFileContext.PreconditionState.PreconditionFailed;
        foreach (EntityTagHeaderValue entityTagHeaderValue in (IEnumerable<EntityTagHeaderValue>) ifMatch)
        {
          if (entityTagHeaderValue.Equals((object) EntityTagHeaderValue.Any) || entityTagHeaderValue.Compare(this._etag, false))
          {
            this._ifMatchState = OssStaticFileContext.PreconditionState.ShouldProcess;
            break;
          }
        }
      }
      IList<EntityTagHeaderValue> ifNoneMatch = this._requestHeaders.IfNoneMatch;
      if (ifNoneMatch == null || !ifNoneMatch.Any<EntityTagHeaderValue>())
        return;
      this._ifNoneMatchState = OssStaticFileContext.PreconditionState.ShouldProcess;
      foreach (EntityTagHeaderValue entityTagHeaderValue in (IEnumerable<EntityTagHeaderValue>) ifNoneMatch)
      {
        if (entityTagHeaderValue.Equals((object) EntityTagHeaderValue.Any) || entityTagHeaderValue.Compare(this._etag, false))
        {
          this._ifNoneMatchState = OssStaticFileContext.PreconditionState.NotModified;
          break;
        }
      }
    }

    private void ComputeIfModifiedSince()
    {
      DateTimeOffset utcNow = DateTimeOffset.UtcNow;
      DateTimeOffset? ifModifiedSince = this._requestHeaders.IfModifiedSince;
      DateTimeOffset? nullable1;
      if (ifModifiedSince.HasValue)
      {
        DateTimeOffset? nullable2 = ifModifiedSince;
        DateTimeOffset dateTimeOffset = utcNow;
        if ((nullable2.HasValue ? (nullable2.GetValueOrDefault() <= dateTimeOffset ? 1 : 0) : 0) != 0)
        {
          nullable1 = ifModifiedSince;
          DateTimeOffset lastModified = this._lastModified;
          this._ifModifiedSinceState = nullable1.HasValue && nullable1.GetValueOrDefault() < lastModified ? OssStaticFileContext.PreconditionState.ShouldProcess : OssStaticFileContext.PreconditionState.NotModified;
        }
      }
      DateTimeOffset? ifUnmodifiedSince = this._requestHeaders.IfUnmodifiedSince;
      if (!ifUnmodifiedSince.HasValue)
        return;
      nullable1 = ifUnmodifiedSince;
      DateTimeOffset dateTimeOffset1 = utcNow;
      if ((nullable1.HasValue ? (nullable1.GetValueOrDefault() <= dateTimeOffset1 ? 1 : 0) : 0) == 0)
        return;
      nullable1 = ifUnmodifiedSince;
      DateTimeOffset lastModified1 = this._lastModified;
      this._ifUnmodifiedSinceState = nullable1.HasValue && nullable1.GetValueOrDefault() >= lastModified1 ? OssStaticFileContext.PreconditionState.ShouldProcess : OssStaticFileContext.PreconditionState.PreconditionFailed;
    }

    private void ComputeIfRange()
    {
      RangeConditionHeaderValue ifRange = this._requestHeaders.IfRange;
      if (ifRange == null)
        return;
      DateTimeOffset? lastModified1 = ifRange.LastModified;
      if (lastModified1.HasValue)
      {
        DateTimeOffset lastModified2 = this._lastModified;
        lastModified1 = ifRange.LastModified;
        if ((lastModified1.HasValue ? (lastModified2 > lastModified1.GetValueOrDefault() ? 1 : 0) : 0) == 0)
          return;
        this._isRangeRequest = false;
      }
      else
      {
        if (this._etag == null || ifRange.EntityTag == null || ifRange.EntityTag.Compare(this._etag, true))
          return;
        this._isRangeRequest = false;
      }
    }

    private void ComputeRange()
    {
      if (!this._isGet)
        return;
      (this._isRangeRequest, this._range) = OssStaticFileRangerHelper.ParseRange(this._context, this._requestHeaders, this._length, this._logger);
    }

    public void ApplyResponseHeaders(int statusCode)
    {
      this._response.StatusCode = statusCode;
      if (statusCode < 400)
      {
        if (!string.IsNullOrEmpty(this._contentType))
          this._response.ContentType = this._contentType;
        this._responseHeaders.LastModified = new DateTimeOffset?(this._lastModified);
        this._responseHeaders.ETag = this._etag;
        this._responseHeaders.Headers["Accept-Ranges"] = (StringValues) "bytes";
      }
      if (statusCode == 200)
        this._response.ContentLength = new long?(this._length);
      this._options.OnPrepareResponse(new OssStaticFileResponseContext()
      {
        Context = this._context,
        File = this._fileInfo
      });
    }

    public OssStaticFileContext.PreconditionState GetPreconditionState()
    {
      return OssStaticFileContext.GetMaxPreconditionState(this._ifMatchState, this._ifNoneMatchState, this._ifModifiedSinceState, this._ifUnmodifiedSinceState);
    }

    private static OssStaticFileContext.PreconditionState GetMaxPreconditionState(
      params OssStaticFileContext.PreconditionState[] states)
    {
      OssStaticFileContext.PreconditionState preconditionState = OssStaticFileContext.PreconditionState.Unspecified;
      for (int index = 0; index < states.Length; ++index)
      {
        if (states[index] > preconditionState)
          preconditionState = states[index];
      }
      return preconditionState;
    }

    public Task SendStatusAsync(int statusCode)
    {
      this.ApplyResponseHeaders(statusCode);
      this._logger.LogHandled(statusCode, this.SubPath);
      return Task.CompletedTask;
    }
    

    public async Task SendAsync()
    {
      this.ApplyResponseHeaders(200);
      string physicalPath = this._fileInfo.PhysicalPath;
      IHttpSendFileFeature httpSendFileFeature = this._context.Features.Get<IHttpSendFileFeature>();
      if (httpSendFileFeature != null && !string.IsNullOrEmpty(physicalPath))
      {
        await httpSendFileFeature.SendFileAsync(physicalPath, 0L, new long?(this._length), CancellationToken.None);
      }
      else
      {
        try
        {
          using (Stream readStream = await this._fileInfo.CreateReadStreamAsync())
            await StreamCopyOperation.CopyToAsync(readStream, this._response.Body, new long?(this._length), 65536, this._context.RequestAborted);
        }
        catch (OperationCanceledException ex)
        {
          this._logger.LogWriteCancelled((Exception) ex);
          this._context.Abort();
        }
      }
    }

    internal async Task SendRangeAsync()
    {
      if (this._range == null)
      {
        this._responseHeaders.ContentRange = new ContentRangeHeaderValue(this._length);
        this.ApplyResponseHeaders(416);
        this._logger.LogRangeNotSatisfiable(this.SubPath);
      }
      else
      {
        long start;
        long length;
        this._responseHeaders.ContentRange = this.ComputeContentRange(this._range, out start, out length);
        this._response.ContentLength = new long?(length);
        this.ApplyResponseHeaders(206);
        string physicalPath = this._fileInfo.PhysicalPath;
        IHttpSendFileFeature httpSendFileFeature = this._context.Features.Get<IHttpSendFileFeature>();
        if (httpSendFileFeature != null && !string.IsNullOrEmpty(physicalPath))
        {
          this._logger.LogSendingFileRange(this._response.Headers["Content-Range"], physicalPath);
          await httpSendFileFeature.SendFileAsync(physicalPath, start, new long?(length), CancellationToken.None);
        }
        else
        {
          try
          {
            using (Stream readStream = await this._fileInfo.CreateReadStreamAsync())
            {
              readStream.Seek(start, SeekOrigin.Begin);
              this._logger.LogCopyingFileRange(this._response.Headers["Content-Range"], this.SubPath);
              await StreamCopyOperation.CopyToAsync(readStream, this._response.Body, new long?(length), this._context.RequestAborted);
            }
          }
          catch (OperationCanceledException ex)
          {
            this._logger.LogWriteCancelled((Exception) ex);
            this._context.Abort();
          }
        }
      }
    }

    private ContentRangeHeaderValue ComputeContentRange(
      RangeItemHeaderValue range,
      out long start,
      out long length)
    {
      start = range.From.Value;
      long to = range.To.Value;
      length = to - start + 1L;
      return new ContentRangeHeaderValue(start, to, this._length);
    }

    public enum PreconditionState
    {
      Unspecified,
      NotModified,
      ShouldProcess,
      PreconditionFailed,
    }

    public void Dispose()
    {
      if (_fileInfo != null)
      {
        this._fileInfo.Dispose();
      }
    }
  }


}