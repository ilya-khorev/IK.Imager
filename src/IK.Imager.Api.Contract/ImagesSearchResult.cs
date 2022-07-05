namespace IK.Imager.Api.Contract;

/// <summary>
/// Model with set of images
/// </summary>
public class ImagesSearchResult
{
    /// <summary>
    /// Set of images
    /// </summary>
    public List<ImageFullInfoWithThumbnails> Images { get; set; }
}