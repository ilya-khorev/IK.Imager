using System.Text.Json;
using IK.Imager.Storage.Abstractions.Models;
using Microsoft.Azure.Cosmos;

namespace IK.Imager.Storage.CosmosDb;

/// <summary>
/// The serializer every client that reaches the metadata container has to be built with.
/// </summary>
/// <remarks>
/// The SDK default serializer reads Newtonsoft attributes, which <see cref="ImageMetadata"/> no longer
/// carries. A client left on that default would write documents with no "id" at all, so this is not a
/// tuning choice - build every client through <see cref="CreateClientOptions"/>.
///
/// The options are deliberately bare. System.Text.Json and the serializer it replaces both write member
/// names unchanged, enums as numbers, nulls as nulls and dates as ISO 8601, so documents written before
/// this change still read back and the shape stays what "/TenantId" - the first level of the partition
/// key - points at. A naming policy here would silently make every stored document unreadable.
/// </remarks>
public static class ImageMetadataSerialization
{
    //shared: System.Text.Json caches its type metadata per options instance
    private static readonly JsonSerializerOptions Options = new();

    /// <summary>
    /// Client options carrying the serializer. Everything else is left at the SDK default -
    /// the integration tests add the emulator settings on top.
    /// </summary>
    public static CosmosClientOptions CreateClientOptions() =>
        new() { UseSystemTextJsonSerializerWithOptions = Options };
}
