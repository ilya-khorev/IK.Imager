using System;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Options;

#pragma warning disable 1591

namespace IK.Imager.Core.Cdn;

public class ImageUrlBuilder(IImageBlobRepository blobRepository, IOptions<CdnSettings> cdnSettings) : IImageUrlBuilder
{
    /// <inheritdoc />
    public Uri Build(string imageName, ImageVariant variant)
    {
        var blobUri = blobRepository.GetImageUri(imageName, variant);

        var cdnUri = cdnSettings.Value.Uri;
        if (cdnUri == null)
            return blobUri;

        return new UriBuilder(blobUri)
        {
            Host = cdnUri.Host,
            Scheme = cdnUri.Scheme
        }.Uri;
    }
}
