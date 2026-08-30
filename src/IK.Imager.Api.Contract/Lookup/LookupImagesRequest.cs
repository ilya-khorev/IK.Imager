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
}
