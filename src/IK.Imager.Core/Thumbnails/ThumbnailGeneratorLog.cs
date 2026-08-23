using Microsoft.Extensions.Logging;

namespace IK.Imager.Core.Thumbnails;

internal static partial class ThumbnailGeneratorLog
{
    [LoggerMessage(EventId = 1100, Level = LogLevel.Warning,
        Message = "No metadata for image {ImageId} in group {ImageGroup}. Not generating thumbnails.")]
    public static partial void ImageNotFound(this ILogger logger, string imageId, string? imageGroup);

    [LoggerMessage(EventId = 1101, Level = LogLevel.Debug,
        Message = "Read metadata for image {ImageId}, {Width}x{Height}.")]
    public static partial void ImageMetadataRead(this ILogger logger, string imageId, int width, int height);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Information,
        Message = "Image {ImageId} is {Width} wide, narrower than the smallest thumbnail. Nothing to generate.")]
    public static partial void ImageSmallerThanTargetWidth(this ILogger logger, string imageId, int width);

    [LoggerMessage(EventId = 1103, Level = LogLevel.Debug,
        Message = "Downloaded the original of image {ImageId} from storage.")]
    public static partial void OriginalImageDownloaded(this ILogger logger, string imageId);

    [LoggerMessage(EventId = 1104, Level = LogLevel.Debug,
        Message = "Resized image {ImageId} to {TargetWidth} wide: {Width}x{Height}, {SizeBytes} bytes.")]
    public static partial void ImageResized(this ILogger logger, string imageId, int targetWidth, int width, int height, long sizeBytes);

    [LoggerMessage(EventId = 1105, Level = LogLevel.Information,
        Message = "Generated {ThumbnailCount} thumbnail(s) for image {ImageId}.")]
    public static partial void ThumbnailsGenerated(this ILogger logger, int thumbnailCount, string imageId);
}
