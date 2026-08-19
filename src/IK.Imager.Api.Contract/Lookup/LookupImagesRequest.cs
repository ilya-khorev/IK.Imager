namespace IK.Imager.Api.Contract.Lookup;

/// <summary>
/// Model with identifiers needed to look up images
/// </summary>
public record LookupImagesRequest
{
    /// <summary>
    /// Image identifiers to look up.
    /// Maximum 200 identifiers are allowed to be passed into one request.
    /// </summary>
    public string[] ImageIds { get; init; } = null!;

    /// <summary>
    /// Image group, which was specified when uploading these images.
    ///
    /// If the images belong to different image groups, you may split this request into several requests with unique image group.
    /// This parameter is optional. However, specifying this value will increase this operation performance.
    /// </summary>
    public string? ImageGroup { get; init; }
}
