using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Abstractions.Lookup;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Logging;

#pragma warning disable 1591

namespace IK.Imager.Core.Lookup;

public class ImageLookup(
    ILogger<ImageLookup> logger,
    IImageMetadataRepository metadataRepository,
    IImageUrlBuilder imageUrlBuilder) : IImageLookup
{
    public async Task<ImageLookupResult> LookupByIds(string[] imageIds, string? imageGroup, CancellationToken cancellationToken)
    {
        var imagesMetadata = await metadataRepository.GetMetadata(imageIds, imageGroup, cancellationToken);

        var images = imagesMetadata.Select(ToImageDetails).ToList();

        logger.ImagesFound(images.Count, imageIds.Length);

        return new ImageLookupResult
        {
            Images = images
        };
    }

    //todo if an image was added a long time ago and there are not any thumbnails, it's worth sending a new event to generate them
    private ImageDetailsWithThumbnails ToImageDetails(ImageMetadata imageMetadata) =>
        new()
        {
            Id = imageMetadata.Id,
            Bytes = imageMetadata.SizeBytes,
            Hash = imageMetadata.MD5Hash,
            Height = imageMetadata.Height,
            Width = imageMetadata.Width,
            Tags = imageMetadata.Tags ?? new Dictionary<string, string>(),
            Url = imageUrlBuilder.Build(imageMetadata.BlobPath, ImageVariant.Original),
            DateAdded = imageMetadata.DateAddedUtc,
            MimeType = imageMetadata.MimeType,
            Thumbnails = imageMetadata.Thumbnails?.Select(ToThumbnailDetails).ToList() ?? []
        };

    private ImageDetails ToThumbnailDetails(ImageThumbnail thumbnail) =>
        new()
        {
            Id = thumbnail.Id,
            Bytes = thumbnail.SizeBytes,
            Hash = thumbnail.MD5Hash,
            Height = thumbnail.Height,
            Width = thumbnail.Width,
            DateAdded = thumbnail.DateAddedUtc,
            MimeType = thumbnail.MimeType,
            Url = imageUrlBuilder.Build(thumbnail.BlobPath, ImageVariant.Thumbnail)
        };
}
