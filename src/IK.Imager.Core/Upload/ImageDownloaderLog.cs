using System;
using Microsoft.Extensions.Logging;

namespace IK.Imager.Core.Upload;

internal static partial class ImageDownloaderLog
{
    public static void DownloadFailed(this ILogger logger, Exception exception, string imageUrl) =>
        DownloadFailedCore(logger, exception, UrlRedactor.Redact(imageUrl));

    public static void DownloadTooLarge(this ILogger logger, string imageUrl, int maxSizeBytes) =>
        DownloadTooLargeCore(logger, UrlRedactor.Redact(imageUrl), maxSizeBytes);

    public static void DownloadRefused(this ILogger logger, string imageUrl, int statusCode) =>
        DownloadRefusedCore(logger, UrlRedactor.Redact(imageUrl), statusCode);

    public static void DownloadSchemeRefused(this ILogger logger, string imageUrl) =>
        DownloadSchemeRefusedCore(logger, UrlRedactor.Redact(imageUrl));

    public static void TooManyRedirects(this ILogger logger, string imageUrl, int maxRedirects) =>
        TooManyRedirectsCore(logger, UrlRedactor.Redact(imageUrl), maxRedirects);

    public static void DownloadUrlNotAbsolute(this ILogger logger, string imageUrl) =>
        DownloadUrlNotAbsoluteCore(logger, UrlRedactor.Redact(imageUrl));

    [LoggerMessage(EventId = 1050, Level = LogLevel.Warning, Message = "Could not download an image from {ImageUrl}.")]
    private static partial void DownloadFailedCore(ILogger logger, Exception exception, string imageUrl);

    [LoggerMessage(EventId = 1051, Level = LogLevel.Warning,
        Message = "The image at {ImageUrl} is larger than the {MaxSizeBytes} byte limit.")]
    private static partial void DownloadTooLargeCore(ILogger logger, string imageUrl, int maxSizeBytes);

    [LoggerMessage(EventId = 1052, Level = LogLevel.Warning,
        Message = "Could not download an image from {ImageUrl}, the server answered {StatusCode}.")]
    private static partial void DownloadRefusedCore(ILogger logger, string imageUrl, int statusCode);

    [LoggerMessage(EventId = 1053, Level = LogLevel.Warning,
        Message = "Could not download an image from {ImageUrl}, only http and https urls are downloaded.")]
    private static partial void DownloadSchemeRefusedCore(ILogger logger, string imageUrl);

    [LoggerMessage(EventId = 1054, Level = LogLevel.Warning,
        Message = "Could not download an image from {ImageUrl}, it redirected more than {MaxRedirects} times.")]
    private static partial void TooManyRedirectsCore(ILogger logger, string imageUrl, int maxRedirects);

    [LoggerMessage(EventId = 1055, Level = LogLevel.Warning,
        Message = "Could not download an image, {ImageUrl} is not an absolute url.")]
    private static partial void DownloadUrlNotAbsoluteCore(ILogger logger, string imageUrl);
}
