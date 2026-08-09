namespace IK.Imager.Api.Contract.ImageUpload;

/// <summary>
/// Model that represent a request for uploading a new image
/// </summary>
public record UploadImageRequest: UploadImageRequestBase
{
    /// <summary>
    /// Absolute image url
    /// </summary>
    public string ImageUrl { get; init; } = null!;
}