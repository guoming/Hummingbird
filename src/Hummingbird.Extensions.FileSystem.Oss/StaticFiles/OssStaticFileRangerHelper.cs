using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Hummingbird.Extensions.FileSystem.Oss.StaticFile
{


internal static class OssStaticFileRangerHelper
  {
    /// <summary>
    /// Returns the normalized form of the requested range if the Range Header in the <see cref="P:Microsoft.AspNetCore.Http.HttpContext.Request" /> is valid.
    /// </summary>
    /// <param name="context">The <see cref="T:Microsoft.AspNetCore.Http.HttpContext" /> associated with the request.</param>
    /// <param name="requestHeaders">The <see cref="T:Microsoft.AspNetCore.Http.Headers.RequestHeaders" /> associated with the given <paramref name="context" />.</param>
    /// <param name="length">The total length of the file representation requested.</param>
    /// <param name="logger">The <see cref="T:Microsoft.Extensions.Logging.ILogger" />.</param>
    /// <returns>A boolean value which represents if the <paramref name="requestHeaders" /> contain a single valid
    /// range request. A <see cref="T:Microsoft.Net.Http.Headers.RangeItemHeaderValue" /> which represents the normalized form of the
    /// range parsed from the <paramref name="requestHeaders" /> or <c>null</c> if it cannot be normalized.</returns>
    /// <remark>If the Range header exists but cannot be parsed correctly, or if the provided length is 0, then the range request cannot be satisfied (status 416).
    /// This results in (<c>true</c>,<c>null</c>) return values.</remark>
    public static (bool isRangeRequest, RangeItemHeaderValue range) ParseRange(
      HttpContext context,
      RequestHeaders requestHeaders,
      long length,
      ILogger logger)
    {
      StringValues header = context.Request.Headers["Range"];
      if (StringValues.IsNullOrEmpty(header))
      {
        logger.LogTrace("Range header's value is empty.");
        return (false, (RangeItemHeaderValue) null);
      }
      if (header.Count > 1 || header[0].IndexOf(',') >= 0)
      {
        logger.LogDebug("Multiple ranges are not supported.");
        return (false, (RangeItemHeaderValue) null);
      }
      RangeHeaderValue range = requestHeaders.Range;
      if (range == null)
      {
        logger.LogDebug("Range header's value is invalid.");
        return (false, (RangeItemHeaderValue) null);
      }
      ICollection<RangeItemHeaderValue> ranges = range.Ranges;
      if (ranges == null)
      {
        logger.LogDebug("Range header's value is invalid.");
        return (false, (RangeItemHeaderValue) null);
      }
      if (ranges.Count == 0)
        return (true, (RangeItemHeaderValue) null);
      return length == 0L ? (true, (RangeItemHeaderValue) null) : (true, OssStaticFileRangerHelper.NormalizeRange(ranges.SingleOrDefault<RangeItemHeaderValue>(), length));
    }

    internal static RangeItemHeaderValue NormalizeRange(RangeItemHeaderValue range, long length)
    {
      long? from = range.From;
      long? to = range.To;
      if (from.HasValue)
      {
        if (from.Value >= length)
          return (RangeItemHeaderValue) null;
        if (!to.HasValue || to.Value >= length)
          to = new long?(length - 1L);
      }
      else
      {
        if (to.Value == 0L)
          return (RangeItemHeaderValue) null;
        long num1 = Math.Min(to.Value, length);
        from = new long?(length - num1);
        long? nullable1 = from;
        long num2 = num1;
        long? nullable2 = nullable1.HasValue ? new long?(nullable1.GetValueOrDefault() + num2) : new long?();
        long num3 = 1;
        long? nullable3;
        if (!nullable2.HasValue)
        {
          nullable1 = new long?();
          nullable3 = nullable1;
        }
        else
          nullable3 = new long?(nullable2.GetValueOrDefault() - num3);
        to = nullable3;
      }
      return new RangeItemHeaderValue(from, to);
    }
  }
}