using Microsoft.Extensions.Logging;

namespace IK.Imager.Api.IntegrationEvents.EventHandling;

/// <summary>
/// The three consumers share one class because they share one EventId range, 2000-2099.
/// </summary>
internal static partial class ConsumerLog
{
    [LoggerMessage(EventId = 2000, Level = LogLevel.Debug,
        Message = "Generating thumbnails for image {ImageId}.")]
    public static partial void ThumbnailJobReceived(this ILogger logger, string imageId);

    [LoggerMessage(EventId = 2010, Level = LogLevel.Debug,
        Message = "Removing the files of image {ImageId}, {ThumbnailCount} thumbnail(s).")]
    public static partial void RemoveFilesJobReceived(this ILogger logger, string imageId, int thumbnailCount);

    [LoggerMessage(EventId = 2020, Level = LogLevel.Debug,
        Message = "Purging {UriCount} uri(s) of image {ImageId} from the CDN.")]
    public static partial void PurgeJobReceived(this ILogger logger, int uriCount, string imageId);
}
