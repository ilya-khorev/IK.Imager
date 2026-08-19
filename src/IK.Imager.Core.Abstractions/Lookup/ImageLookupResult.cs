using System.Collections.Generic;
using IK.Imager.Core.Abstractions.Models;

namespace IK.Imager.Core.Abstractions.Lookup;

public class ImageLookupResult
{
    /// <summary>
    /// Set of images
    /// </summary>
    public List<ImageDetailsWithThumbnails> Images { get; set; } = new();
}
