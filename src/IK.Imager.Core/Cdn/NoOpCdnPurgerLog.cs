using Microsoft.Extensions.Logging;

namespace IK.Imager.Core.Cdn;

internal static partial class NoOpCdnPurgerLog
{
    [LoggerMessage(EventId = 1400, Level = LogLevel.Debug,
        Message = "No CDN purger is registered - not purging {UriCount} uri(s).")]
    public static partial void NotPurging(this ILogger logger, int uriCount);
}
