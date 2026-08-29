using System;
using System.Security.Cryptography;
using System.Text;
using IK.Imager.Core.Abstractions;

namespace IK.Imager.Core;

public class ImageNameGenerator : IImageNameGenerator
{
    /// <summary>
    /// 128 bits, matching the strength of a generated image id.
    /// </summary>
    private const int UniquePrefixLength = 32;

    /// <inheritdoc />
    public string NewImageId()
    {
        //since all images are publicly available by url, image path must be random and big enough
        //so, for simplicity just concatenating guid and part of another guid
        return (Guid.NewGuid()
                + Guid.NewGuid().ToString().Substring(0, 6))
            .Replace("-", "");
    }

    /// <inheritdoc />
    //RandomNumberGenerator rather than Guid, because this segment's only job is to be unguessable and that
    //should be visible in the code rather than inferred from how Guid.NewGuid happens to be implemented
    public string NewUniquePrefix() => RandomNumberGenerator.GetHexString(UniquePrefixLength, lowercase: true);

    /// <inheritdoc />
    public string BuildBlobPath(string tenantId, string? collection, string? uniquePrefix, string imageId, string extension)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantId);
        ArgumentException.ThrowIfNullOrEmpty(imageId);

        var path = new StringBuilder(tenantId).Append('/');

        //the collection goes before the prefix, so that a collection stays a usable blob prefix - putting
        //the random segment first would make every collection-scoped storage operation impossible
        if (!string.IsNullOrEmpty(collection))
            path.Append(collection).Append('/');

        if (!string.IsNullOrEmpty(uniquePrefix))
            path.Append(uniquePrefix).Append('/');

        path.Append(imageId);

        return Append(path, extension);
    }

    /// <inheritdoc />
    public string BuildThumbnailBlobPath(string originalBlobPath, int width, string extension)
    {
        ArgumentException.ThrowIfNullOrEmpty(originalBlobPath);

        var path = new StringBuilder(StemOf(originalBlobPath)).Append('_').Append(width);

        return Append(path, extension);
    }

    private static string Append(StringBuilder path, string extension) =>
        (string.IsNullOrWhiteSpace(extension) ? path : path.Append('.').Append(extension)).ToString();

    /// <summary>
    /// The path without its extension. Only a dot in the last segment is one.
    /// </summary>
    private static string StemOf(string blobPath)
    {
        var lastDot = blobPath.LastIndexOf('.');

        return lastDot > blobPath.LastIndexOf('/') ? blobPath[..lastDot] : blobPath;
    }
}
