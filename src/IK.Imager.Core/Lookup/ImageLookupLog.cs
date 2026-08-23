using Microsoft.Extensions.Logging;

namespace IK.Imager.Core.Lookup;

internal static partial class ImageLookupLog
{
    //a read that changed nothing, on the highest frequency endpoint - Debug rather than Information
    [LoggerMessage(EventId = 1300, Level = LogLevel.Debug,
        Message = "Found {FoundCount} image(s) for {RequestedCount} requested id(s).")]
    public static partial void ImagesFound(this ILogger logger, int foundCount, int requestedCount);
}
