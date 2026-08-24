using IK.Imager.Storage.Abstractions.Models;
using Microsoft.Extensions.Logging;

namespace IK.Imager.Core.Upload;

/// <summary>
/// See <see cref="ImageUploaderLog"/> for the EventId ranges and for why this is a static class.
/// </summary>
internal static partial class ImageInspectorLog
{
    [LoggerMessage(EventId = 1500, Level = LogLevel.Debug, Message = "Checking the image.")]
    public static partial void CheckingImage(this ILogger logger);

    [LoggerMessage(EventId = 1501, Level = LogLevel.Debug,
        Message = "Detected image format {MimeType} ({ImageType}), extension {FileExtension}.")]
    public static partial void ImageFormatDetected(this ILogger logger, string mimeType, ImageType imageType, string fileExtension);

    [LoggerMessage(EventId = 1502, Level = LogLevel.Debug,
        Message = "Read image size {Width}x{Height}, {SizeBytes} bytes, aspect ratio {AspectRatio}.")]
    public static partial void ImageSizeRead(this ILogger logger, int width, int height, long sizeBytes, double aspectRatio);

    [LoggerMessage(EventId = 1503, Level = LogLevel.Warning, Message = "Image rejected: {ValidationErrorKeys}.")]
    public static partial void ImageRejected(this ILogger logger, string validationErrorKeys);
}
