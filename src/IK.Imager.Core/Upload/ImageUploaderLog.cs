using Microsoft.Extensions.Logging;

namespace IK.Imager.Core.Upload;

/*
 EventId ranges, so that two classes never claim the same id. SYSLIB1006 only catches a collision
 inside one class.

 1000-1049 ImageUploaderLog        1500-1599 ImageInspectorLog
 1050-1099 ImageDownloaderLog      2000-2099 the consumers
 1100-1199 ThumbnailGeneratorLog   2100-2199 ImageEventPublisherLog
 1200-1299 ImageDeleterLog         2200-2299 GlobalExceptionHandlerLog
 1300-1399 ImageLookupLog          3000-3099 AzureBlobImageRepositoryLog
 1400-1499 NoOpCdnPurgerLog        3100-3199 CosmosImageMetadataRepositoryLog
                                   4000-4399 the four CDN purgers, 100 each
*/

/// <summary>
/// A static class rather than members on the service: the generator looks for an ILogger field, and a
/// primary constructor parameter is not one, so an instance LoggerMessage there fails with SYSLIB1019.
/// </summary>
internal static partial class ImageUploaderLog
{
    [LoggerMessage(EventId = 1004, Level = LogLevel.Debug,
        Message = "Uploaded the image to blob storage, {ImageId} as {BlobPath}.")]
    public static partial void UploadedToBlobStorage(this ILogger logger, string imageId, string blobPath);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Information,
        Message = "Saved image {ImageId}, {SizeBytes} bytes.")]
    public static partial void UploadFinished(this ILogger logger, string imageId, long sizeBytes);

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
