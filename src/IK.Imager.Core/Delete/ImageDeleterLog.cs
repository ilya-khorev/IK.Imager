using Microsoft.Extensions.Logging;

namespace IK.Imager.Core.Delete;

internal static partial class ImageDeleterLog
{
    [LoggerMessage(EventId = 1200, Level = LogLevel.Debug,
        Message = "Removing the metadata of image {ImageId} in group {ImageGroup}.")]
    public static partial void RemovingMetadata(this ILogger logger, string imageId, string? imageGroup);

    [LoggerMessage(EventId = 1201, Level = LogLevel.Information, Message = "Removed the metadata of image {ImageId}.")]
    public static partial void MetadataRemoved(this ILogger logger, string imageId);

    [LoggerMessage(EventId = 1202, Level = LogLevel.Debug,
        Message = "Removing the files of image {ImageId}: {BlobPath} and thumbnails {ThumbnailBlobPaths}.")]
    public static partial void RemovingFiles(this ILogger logger, string imageId, string? blobPath, string thumbnailBlobPaths);

    [LoggerMessage(EventId = 1203, Level = LogLevel.Information,
        Message = "Deleted the files of image {ImageId}: original deleted {OriginalDeleted}, {DeletedCount} of {ThumbnailCount} thumbnails.")]
    public static partial void FilesDeleted(this ILogger logger, string imageId, bool originalDeleted, int deletedCount, int thumbnailCount);
}
