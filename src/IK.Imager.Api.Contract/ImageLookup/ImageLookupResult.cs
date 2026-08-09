namespace IK.Imager.Api.Contract.ImageLookup;

/// <summary>
/// Model with set of images
/// </summary>
public record ImageLookupResult
{
    /// <summary>
    /// Set of images
    /// </summary>
    public List<ImageFullInfoWithThumbnails> Images { get; init; } = new();
}
