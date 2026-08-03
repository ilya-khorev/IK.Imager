using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CoreModels = IK.Imager.Core.Abstractions.Models;
using Contract = IK.Imager.Api.Contract;

namespace IK.Imager.Api.Mapping;

/// <summary>
/// Maps the core models returned by the handlers onto the public API contract models.
/// </summary>
public static class ContractMappingExtensions
{
    /// <summary>
    /// Maps a core image info model onto its contract counterpart. Returns null for a null source.
    /// </summary>
    [return: NotNullIfNotNull(nameof(source))]
    public static Contract.ImageInfo? ToContract(this CoreModels.ImageInfo? source)
    {
        if (source == null)
            return null;

        return new Contract.ImageInfo
        {
            Id = source.Id,
            Url = source.Url.ToString(),
            Hash = source.Hash,
            DateAdded = source.DateAdded,
            Width = source.Width,
            Height = source.Height,
            Bytes = source.Bytes,
            MimeType = source.MimeType
        };
    }

    /// <summary>
    /// Maps a core image info model with its thumbnails onto the contract counterpart. Returns null for a null source.
    /// </summary>
    [return: NotNullIfNotNull(nameof(source))]
    public static Contract.ImageFullInfoWithThumbnails? ToContract(this CoreModels.ImageFullInfoWithThumbnails? source)
    {
        if (source == null)
            return null;

        return new Contract.ImageFullInfoWithThumbnails
        {
            Id = source.Id,
            Url = source.Url.ToString(),
            Hash = source.Hash,
            DateAdded = source.DateAdded,
            Width = source.Width,
            Height = source.Height,
            Bytes = source.Bytes,
            MimeType = source.MimeType,
            Tags = source.Tags,
            Thumbnails = source.Thumbnails.Select(x => x.ToContract()).ToList()
        };
    }

    /// <summary>
    /// Maps a core search result onto the contract counterpart. Returns null for a null source.
    /// </summary>
    [return: NotNullIfNotNull(nameof(source))]
    public static Contract.ImagesSearchResult? ToContract(this CoreModels.ImagesSearchResult? source)
    {
        if (source == null)
            return null;

        return new Contract.ImagesSearchResult
        {
            Images = source.Images.Select(x => x.ToContract()).ToList()
        };
    }
}
