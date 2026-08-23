using System;
using Microsoft.Extensions.Logging;

namespace IK.Imager.Cdn.Fastly;

internal static partial class FastlyCdnPurgerLog
{
    [LoggerMessage(EventId = 4200, Level = LogLevel.Information,
        Message = "Fastly accepted a purge of {UriCount} uri(s).")]
    public static partial void Purged(this ILogger logger, int uriCount);

    [LoggerMessage(EventId = 4201, Level = LogLevel.Error,
        Message = "Fastly returned {StatusCode} for a purge of {ContentUri}.")]
    public static partial void PurgeFailed(this ILogger logger, int statusCode, Uri contentUri);
}
