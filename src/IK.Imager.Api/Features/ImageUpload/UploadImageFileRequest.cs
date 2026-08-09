using IK.Imager.Api.Contract.ImageUpload;
using Microsoft.AspNetCore.Http;

namespace IK.Imager.Api.Features.ImageUpload;

/// <summary>
/// Model that represent a request for uploading a new image
/// </summary>
public record UploadImageFileRequest: UploadImageRequestBase
{
    /// <summary>
    /// File sent as a part of form
    /// </summary>
    public IFormFile File { get; init; } = null!;
}
