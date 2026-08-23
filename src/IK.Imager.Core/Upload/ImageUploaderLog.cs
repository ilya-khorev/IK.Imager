using IK.Imager.Storage.Abstractions.Models;
using Microsoft.Extensions.Logging;

namespace IK.Imager.Core.Upload;

/*
 EventId ranges, so that two classes never claim the same id. SYSLIB1006 only catches a collision
 inside one class.

 1000-1049 ImageUploaderLog        2000-2099 the consumers
 1050-1099 ImageDownloaderLog      2100-2199 ImageEventPublisherLog
 1100-1199 ThumbnailGeneratorLog   2200-2299 GlobalExceptionHandlerLog
 1200-1299 ImageDeleterLog         3000-3099 AzureBlobImageRepositoryLog
 1300-1399 ImageLookupLog          3100-3199 CosmosImageMetadataRepositoryLog
 1400-1499 NoOpCdnPurgerLog        4000-4399 the four CDN purgers, 100 each
*/

/// <summary>
/// A static class rather than members on the service: the generator looks for an ILogger field, and a
/// primary constructor parameter is not one, so an instance LoggerMessage there fails with SYSLIB1019.
/// </summary>
internal static partial class ImageUploaderLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Debug, Message = "Checking the image.")]
    public static partial void CheckingImage(this ILogger logger);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Debug,
        Message = "Detected image format {MimeType} ({ImageType}), extension {FileExtension}.")]
    public static partial void ImageFormatDetected(this ILogger logger, string mimeType, ImageType imageType, string fileExtension);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Debug,
        Message = "Read image size {Width}x{Height}, {SizeBytes} bytes, aspect ratio {AspectRatio}.")]
    public static partial void ImageSizeRead(this ILogger logger, int width, int height, long sizeBytes, double aspectRatio);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Warning, Message = "Image rejected: {ValidationErrorKeys}.")]
    public static partial void ImageRejected(this ILogger logger, string validationErrorKeys);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Debug,
        Message = "Uploaded the image to blob storage, {ImageId} as {ImageName}.")]
    public static partial void UploadedToBlobStorage(this ILogger logger, string imageId, string imageName);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Information,
        Message = "Saved image {ImageId} in group {ImageGroup}, {SizeBytes} bytes.")]
    public static partial void UploadFinished(this ILogger logger, string imageId, string imageGroup, long sizeBytes);

    //the url is redacted in a wrapper because a LoggerMessage method is partial - there is no body to do it in
    public static void DownloadingByUrl(this ILogger logger, string imageUrl) =>
        DownloadingByUrlCore(logger, UrlRedactor.Redact(imageUrl));

    public static void DownloadedByUrl(this ILogger logger, string imageUrl, long sizeBytes) =>
        DownloadedByUrlCore(logger, UrlRedactor.Redact(imageUrl), sizeBytes);

    public static void NotDownloadedByUrl(this ILogger logger, string imageUrl) =>
        NotDownloadedByUrlCore(logger, UrlRedactor.Redact(imageUrl));

    [LoggerMessage(EventId = 1006, Level = LogLevel.Debug, Message = "Downloading an image from {ImageUrl}.")]
    private static partial void DownloadingByUrlCore(ILogger logger, string imageUrl);

    [LoggerMessage(EventId = 1007, Level = LogLevel.Debug, Message = "Downloaded {SizeBytes} bytes from {ImageUrl}.")]
    private static partial void DownloadedByUrlCore(ILogger logger, string imageUrl, long sizeBytes);

    [LoggerMessage(EventId = 1008, Level = LogLevel.Warning, Message = "Nothing could be downloaded from {ImageUrl}.")]
    private static partial void NotDownloadedByUrlCore(ILogger logger, string imageUrl);
}
