using System;
using IK.Imager.Storage.Abstractions.Models;

namespace IK.Imager.Core.Abstractions.Cdn;

/// <summary>
/// Builds the public url of a stored image.
/// </summary>
/// <remarks>
/// The single place that decides what the outside world is given for a blob. Urls are never stored -
/// <c>ImageMetadata</c> holds only the blob name - so every url in the system comes from here, and a CDN
/// host, or later a signature, is applied once when the url is created. An earlier version built a raw
/// blob url and then had decorators over the upload and lookup services patch it afterwards, which is
/// also what forced the core models to stay mutable.
/// </remarks>
public interface IImageUrlBuilder
{
    /// <summary>
    /// Returns the url a client should use to fetch the given image.
    /// </summary>
    /// <param name="blobPath">Blob path, as stored in <c>ImageMetadata.BlobPath</c>.</param>
    /// <param name="variant">Original or thumbnail.</param>
    Uri Build(string blobPath, ImageVariant variant);
}
