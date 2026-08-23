using System;
using Microsoft.Extensions.Logging;

namespace IK.Imager.Api.ExceptionHandling;

internal static partial class GlobalExceptionHandlerLog
{
    //the caller's mistake, answered with a 400 - a Warning without a stack trace, not an Error
    [LoggerMessage(EventId = 2200, Level = LogLevel.Warning,
        Message = "Rejected a request to {RequestPath}: {Reason}")]
    public static partial void RequestRejected(this ILogger logger, string requestPath, string reason);

    [LoggerMessage(EventId = 2201, Level = LogLevel.Error, Message = "Unhandled exception serving {RequestPath}.")]
    public static partial void UnhandledException(this ILogger logger, Exception exception, string requestPath);
}
