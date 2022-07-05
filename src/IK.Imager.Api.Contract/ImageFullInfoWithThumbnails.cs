namespace IK.Imager.Api.Contract;

/// <summary>
/// Model containing information about image and its thumbnails
/// </summary>
public class ImageFullInfoWithThumbnails: ImageInfo
{
    /// <summary>
    /// Additional information associated with an image in arbitrary form of key-value dictionary
    /// </summary>
    public IDictionary<string, string> Tags { get; set; }
        
    /// <summary>
    /// Image thumbnails sorted by smallest to the biggest
    /// </summary>
    public List<ImageInfo> Thumbnails { get; set; } 
}