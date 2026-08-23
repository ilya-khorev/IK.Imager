using Microsoft.Extensions.Logging;

namespace IK.Imager.Cdn.Cloudflare;

internal static partial class CloudflareCdnPurgerLog
{
    [LoggerMessage(EventId = 4000, Level = LogLevel.Information,
        Message = "Cloudflare accepted a purge of {UriCount} uri(s) in zone {ZoneId}.")]
    public static partial void Purged(this ILogger logger, int uriCount, string zoneId);

    [LoggerMessage(EventId = 4001, Level = LogLevel.Error,
        Message = "Cloudflare returned {StatusCode} for a purge of {UriCount} uri(s) in zone {ZoneId}.")]
    public static partial void PurgeFailed(this ILogger logger, int statusCode, int uriCount, string zoneId);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Error,
        Message = "Cloudflare rejected a purge of {UriCount} uri(s) in zone {ZoneId}: {PurgeErrors}.")]
    public static partial void PurgeRejected(this ILogger logger, int uriCount, string zoneId, string purgeErrors);
}
