namespace IK.Imager.Api.Contract.Lookup;

/// <summary>
/// Model with set of images
/// </summary>
public record LookupImagesResult
{
    /// <summary>
    /// Set of images
    /// </summary>
    public List<ImageWithThumbnails> Images { get; init; } = new();
}
