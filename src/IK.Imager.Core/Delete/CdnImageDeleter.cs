using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Abstractions.Delete;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;

#pragma warning disable 1591

namespace IK.Imager.Core.Delete;

/// <summary>
/// Purges the CDN once <see cref="ImageDeleter"/> has removed the blobs. Wired in AddImagerCore - the
/// concrete service is registered by its own type, and <see cref="IImageDeleter"/> resolves to this
/// decorator around it.
/// </summary>
public class CdnImageDeleter(
    IImageDeleter inner,
    IImageBlobRepository blobRepository,
    ICdnUrlRewriter cdnUrlRewriter,
    ICdnPurger cdnPurger) : IImageDeleter
{
    //nothing to purge yet - the blobs stay until the ImageMetadataDeleted event is consumed
    public Task<bool> DeleteMetadata(string imageId, string? imageGroup, CancellationToken cancellationToken) =>
        inner.DeleteMetadata(imageId, imageGroup, cancellationToken);

    public async Task DeleteFiles(string imageId, string? imageName, string[] thumbnailNames, CancellationToken cancellationToken)
    {
        var contentUris = CollectContentUris(imageName, thumbnailNames);

        await inner.DeleteFiles(imageId, imageName, thumbnailNames, cancellationToken);

        //purging before the blobs are gone makes the edge re-fetch them and cache them again
        await cdnPurger.Purge(contentUris, cancellationToken);
    }

    private List<Uri> CollectContentUris(string? imageName, string[] thumbnailNames)
    {
        var contentUris = new List<Uri>(thumbnailNames.Length + 1);

        if (!string.IsNullOrEmpty(imageName))
            contentUris.Add(CdnUriOf(imageName, ImageVariant.Original));

        foreach (var thumbnailName in thumbnailNames)
            contentUris.Add(CdnUriOf(thumbnailName, ImageVariant.Thumbnail));

        return contentUris;
    }

    private Uri CdnUriOf(string imageName, ImageVariant variant) =>
        cdnUrlRewriter.Rewrite(blobRepository.GetImageUri(imageName, variant));
}
