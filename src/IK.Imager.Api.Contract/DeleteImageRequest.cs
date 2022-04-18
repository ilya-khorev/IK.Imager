#pragma warning disable 8618
namespace IK.Imager.Api.Contract;

/// <summary>
/// Model with identifiers needed to remove an image
/// </summary>
public class DeleteImageRequest
{
    /// <summary>
    /// Image identifier
    /// </summary>
    public string ImageId { get; set; }
        
    /// <summary>
    /// Image group, that was specified when the image was uploaded.
    /// This parameter is optional, however, specifying this value will increase this operation performance.
    /// </summary>
    public string? ImageGroup { get; set; }
}