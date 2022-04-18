namespace IK.Imager.Api.Contract;

/// <summary>
/// Model with identifiers needed to search for images
/// </summary>
public class SearchImagesByIdRequest
{
    /// <summary>
    /// Image identifiers to search.
    /// Maximum 200 identifiers are allowed to be passed into one request.
    /// </summary>
    public string[] ImageIds { get; set; }
        
    /// <summary>
    /// Image group, which was specified when uploading these images.
    /// 
    /// If the images belong to different image groups, you may split this request into several requests with unique image group.
    /// This parameter is optional. However, specifying this value will increase this operation performance.
    /// </summary>
    public string? ImageGroup { get; set; }
}