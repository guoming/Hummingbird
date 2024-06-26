using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Hummingbird.Extensions.FileSystem.Oss.StaticFile
{ /// <summary>
  /// Defines *all* the logger messages produced by static files
  /// </summary>
  internal static class OssStaticFileLoggerExtensions
  {
    private static Action<ILogger, string, Exception> _logMethodNotSupported = LoggerMessage.Define<string>(LogLevel.Debug, (EventId) 1, "{Method} requests are not supported");
    private static Action<ILogger, string, string, Exception> _logFileServed = LoggerMessage.Define<string, string>(LogLevel.Information, (EventId) 2, "Sending file. Request path: '{VirtualPath}'. Physical path: '{PhysicalPath}'");
    private static Action<ILogger, string, Exception> _logPathMismatch = LoggerMessage.Define<string>(LogLevel.Debug, (EventId) 3, "The request path {Path} does not match the path filter");
    private static Action<ILogger, string, Exception> _logFileTypeNotSupported = LoggerMessage.Define<string>(LogLevel.Debug, (EventId) 4, "The request path {Path} does not match a supported file type");
    private static Action<ILogger, string, Exception> _logFileNotFound = LoggerMessage.Define<string>(LogLevel.Debug, (EventId) 5, "The request path {Path} does not match an existing file");
    private static Action<ILogger, string, Exception> _logPathNotModified = LoggerMessage.Define<string>(LogLevel.Information, (EventId) 6, "The file {Path} was not modified");
    private static Action<ILogger, string, Exception> _logPreconditionFailed = LoggerMessage.Define<string>(LogLevel.Information, (EventId) 7, "Precondition for {Path} failed");
    private static Action<ILogger, int, string, Exception> _logHandled = LoggerMessage.Define<int, string>(LogLevel.Debug, (EventId) 8, "Handled. Status code: {StatusCode} File: {Path}");
    private static Action<ILogger, string, Exception> _logRangeNotSatisfiable = LoggerMessage.Define<string>(LogLevel.Warning, (EventId) 9, "Range not satisfiable for {Path}");
    private static Action<ILogger, StringValues, string, Exception> _logSendingFileRange = LoggerMessage.Define<StringValues, string>(LogLevel.Information, (EventId) 10, "Sending {Range} of file {Path}");
    private static Action<ILogger, StringValues, string, Exception> _logCopyingFileRange = LoggerMessage.Define<StringValues, string>(LogLevel.Debug, (EventId) 11, "Copying {Range} of file {Path} to the response body");
    private static Action<ILogger, long, string, string, Exception> _logCopyingBytesToResponse = LoggerMessage.Define<long, string, string>(LogLevel.Debug, (EventId) 12, "Copying bytes {Start}-{End} of file {Path} to response body");
    private static Action<ILogger, Exception> _logWriteCancelled = LoggerMessage.Define(LogLevel.Debug, (EventId) 14, "The file transmission was cancelled");

    public static void LogRequestMethodNotSupported(this ILogger logger, string method)
    {
      _logMethodNotSupported(logger, method, (Exception) null);
    }

    public static void LogFileServed(this ILogger logger, string virtualPath, string physicalPath)
    {
      if (string.IsNullOrEmpty(physicalPath))
        physicalPath = "N/A";
      _logFileServed(logger, virtualPath, physicalPath, (Exception) null);
    }

    public static void LogPathMismatch(this ILogger logger, string path)
    {
      _logPathMismatch(logger, path, (Exception) null);
    }

    public static void LogFileTypeNotSupported(this ILogger logger, string path)
    {
      _logFileTypeNotSupported(logger, path, (Exception) null);
    }

    public static void LogFileNotFound(this ILogger logger, string path)
    {
      _logFileNotFound(logger, path, (Exception) null);
    }

    public static void LogPathNotModified(this ILogger logger, string path)
    {
      _logPathNotModified(logger, path, (Exception) null);
    }

    public static void LogPreconditionFailed(this ILogger logger, string path)
    {
      _logPreconditionFailed(logger, path, (Exception) null);
    }

    public static void LogHandled(this ILogger logger, int statusCode, string path)
    {
      _logHandled(logger, statusCode, path, (Exception) null);
    }

    public static void LogRangeNotSatisfiable(this ILogger logger, string path)
    {
      _logRangeNotSatisfiable(logger, path, (Exception) null);
    }

    public static void LogSendingFileRange(this ILogger logger, StringValues range, string path)
    {
      _logSendingFileRange(logger, range, path, (Exception) null);
    }

    public static void LogCopyingFileRange(this ILogger logger, StringValues range, string path)
    {
      _logCopyingFileRange(logger, range, path, (Exception) null);
    }

    public static void LogCopyingBytesToResponse(
      this ILogger logger,
      long start,
      long? end,
      string path)
    {
      _logCopyingBytesToResponse(logger, start, end.HasValue ? end.ToString() : "*", path, (Exception) null);
    }

    public static void LogWriteCancelled(this ILogger logger, Exception ex)
    {
      _logWriteCancelled(logger, ex);
    }
  }
}