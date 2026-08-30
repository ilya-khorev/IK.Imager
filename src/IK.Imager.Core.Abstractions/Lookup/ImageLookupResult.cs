using System.Collections.Generic;

namespace IK.Imager.Core.Abstractions.Lookup;

public record ImageLookupResult
{
    /// <summary>
    /// Set of images
    /// </summary>
    public List<ImageDetailsWithThumbnails> Images { get; init; } = new();
}
