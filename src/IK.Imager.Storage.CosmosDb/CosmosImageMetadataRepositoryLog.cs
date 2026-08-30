using Microsoft.Extensions.Logging;

namespace IK.Imager.Storage.CosmosDb;

/// <summary>
/// The request charge is the only cost signal Cosmos gives, and nothing else in the system reports it.
/// </summary>
internal static partial class CosmosImageMetadataRepositoryLog
{
    [LoggerMessage(EventId = 3100, Level = LogLevel.Debug,
        Message = "Created the metadata of image {ImageId}, {RequestCharge} RU.")]
    public static partial void MetadataCreated(this ILogger logger, string imageId, double requestCharge);

    [LoggerMessage(EventId = 3101, Level = LogLevel.Debug,
        Message = "Read {FoundCount} metadata document(s) for {RequestedCount} id(s), {RequestCharge} RU.")]
    public static partial void MetadataRead(this ILogger logger, int foundCount, int requestedCount, double requestCharge);

    [LoggerMessage(EventId = 3102, Level = LogLevel.Debug,
        Message = "Removed the metadata of image {ImageId}, {RequestCharge} RU.")]
    public static partial void MetadataRemoved(this ILogger logger, string imageId, double requestCharge);

    [LoggerMessage(EventId = 3103, Level = LogLevel.Debug,
        Message = "Updated the metadata of image {ImageId}, {RequestCharge} RU.")]
    public static partial void MetadataUpdated(this ILogger logger, string imageId, double requestCharge);
}
