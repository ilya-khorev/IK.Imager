using IK.Imager.Storage.Abstractions.Models;
using Microsoft.Extensions.Logging;

namespace IK.Imager.Storage.AzureBlobs;

internal static partial class AzureBlobImageRepositoryLog
{
    [LoggerMessage(EventId = 3000, Level = LogLevel.Debug,
        Message = "No {Variant} blob named {BlobPath} in storage.")]
    public static partial void BlobNotFound(this ILogger logger, ImageVariant variant, string blobPath);
}
