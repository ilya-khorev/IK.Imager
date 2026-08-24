using System;
using Microsoft.Extensions.Logging;

namespace IK.Imager.Core.Upload;

internal static partial class ImageDownloaderLog
{
    public static void DownloadFailed(this ILogger logger, Exception exception, string imageUrl) =>
        DownloadFailedCore(logger, exception, UrlRedactor.Redact(imageUrl));

    public static void DownloadTooLarge(this ILogger logger, string imageUrl, int maxSizeBytes) =>
        DownloadTooLargeCore(logger, UrlRedactor.Redact(imageUrl), maxSizeBytes);

    [LoggerMessage(EventId = 1050, Level = LogLevel.Warning, Message = "Could not download an image from {ImageUrl}.")]
    private static partial void DownloadFailedCore(ILogger logger, Exception exception, string imageUrl);

    [LoggerMessage(EventId = 1051, Level = LogLevel.Warning,
        Message = "The image at {ImageUrl} is larger than the {MaxSizeBytes} byte limit.")]
    private static partial void DownloadTooLargeCore(ILogger logger, string imageUrl, int maxSizeBytes);
}
