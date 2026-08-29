#pragma warning disable 8618
namespace IK.Imager.Api.Contract.Delete;

/// <summary>
/// Model with identifiers needed to remove an image
/// </summary>
public record DeleteImageRequest
{
    /// <summary>
    /// Image identifier
    /// </summary>
    public string ImageId { get; init; }
}
