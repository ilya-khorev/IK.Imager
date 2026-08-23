using Microsoft.Extensions.Logging;

namespace IK.Imager.Api.IntegrationEvents;

internal static partial class ImageEventPublisherLog
{
    [LoggerMessage(EventId = 2100, Level = LogLevel.Debug,
        Message = "Published {MessageType} for image {ImageId}.")]
    public static partial void EventPublished(this ILogger logger, string messageType, string imageId);
}
