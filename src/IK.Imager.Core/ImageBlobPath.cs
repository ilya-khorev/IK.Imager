using System;
using System.Text;

namespace IK.Imager.Core;

/// <summary>
/// Builds the blob path of an image and of its thumbnails.
///
/// The path is the delivery url, so everything about it is deliberate:
/// <c>{tenant}/[{collection}/][{prefix}/]{imageId}.{extension}</c>. The tenant has to be there because
/// ids are only unique within one, and the two middle segments are what the caller asked for.
/// </summary>
public static class ImageBlobPath
{
    /// <summary>
    /// Assembles the blob path of an original image. A null or empty collection or prefix contributes
    /// no segment.
    /// </summary>
    public static string Build(string tenantId, string? collection, string? uniquePrefix, string imageId, string extension)
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

    /// <summary>
    /// Assembles the blob path of a thumbnail from the path of its original, as
    /// <c>{original without extension}_{width}.{extension}</c>.
    ///
    /// Derived rather than rebuilt so a thumbnail inherits the tenant, collection and prefix of its
    /// original without having to be told any of them - and so regenerating thumbnails overwrites the
    /// previous set in place instead of orphaning it.
    /// </summary>
    public static string BuildThumbnail(string originalBlobPath, int width, string extension)
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
