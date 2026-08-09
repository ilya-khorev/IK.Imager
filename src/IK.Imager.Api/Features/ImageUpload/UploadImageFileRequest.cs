using IK.Imager.Api.Contract;
using Microsoft.AspNetCore.Http;

namespace IK.Imager.Api.Features.ImageUpload;

/// <summary>
/// Model that represent a request for uploading a new image
/// </summary>
public class UploadImageFileRequest: UploadImageRequestBase
{
    /// <summary>
    /// File sent as a part of form
    /// </summary>
    public IFormFile File { get; set; } = null!;
}
