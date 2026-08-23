using Microsoft.Extensions.Logging;

namespace IK.Imager.Cdn.Akamai;

internal static partial class AkamaiCdnPurgerLog
{
    [LoggerMessage(EventId = 4300, Level = LogLevel.Information,
        Message = "Akamai accepted a purge of {UriCount} uri(s).")]
    public static partial void Purged(this ILogger logger, int uriCount);

    [LoggerMessage(EventId = 4301, Level = LogLevel.Error,
        Message = "Akamai returned {StatusCode} for a purge of {UriCount} uri(s).")]
    public static partial void PurgeFailed(this ILogger logger, int statusCode, int uriCount);
}
