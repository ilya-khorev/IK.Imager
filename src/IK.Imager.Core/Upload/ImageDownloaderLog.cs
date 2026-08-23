using System;
using Microsoft.Extensions.Logging;

namespace IK.Imager.Core.Upload;

internal static partial class ImageDownloaderLog
{
    public static void DownloadFailed(this ILogger logger, Exception exception, string imageUrl) =>
        DownloadFailedCore(logger, exception, UrlRedactor.Redact(imageUrl));

    [LoggerMessage(EventId = 1050, Level = LogLevel.Warning, Message = "Could not download an image from {ImageUrl}.")]
    private static partial void DownloadFailedCore(ILogger logger, Exception exception, string imageUrl);
}
