namespace IK.Imager.Api.Contract.Upload;

/// <summary>
/// Model that represent a request for uploading a new image
/// </summary>
public record UploadImageByUrlRequest : UploadImageRequestBase
{
    /// <summary>
    /// Absolute image url
    /// </summary>
    public string ImageUrl { get; init; } = null!;
}
