using System.Collections.Generic;
using IK.Imager.Core.Abstractions.Models;

namespace IK.Imager.Core.Abstractions.Lookup;

/// <summary>
/// Model containing information about image and its thumbnails
/// </summary>
public record ImageDetailsWithThumbnails : ImageDetails
{
    /// <summary>
    /// Additional information associated with an image in arbitrary form of key-value dictionary.
    /// Optional: an image may carry no tags at all.
    /// </summary>
    public IDictionary<string, string>? Tags { get; init; }

    /// <summary>
    /// Image thumbnails sorted by smallest to the biggest.
    /// Empty while thumbnail generation is still in flight.
    /// </summary>
    public List<ImageDetails> Thumbnails { get; init; } = new();
}
