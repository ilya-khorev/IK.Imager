namespace IK.Imager.Api.Contract.Upload;

/// <summary>
/// Model that represent a request for uploading a new image
/// </summary>
public abstract record UploadImageRequestBase
{
    /// <summary>
    /// Optional label grouping images within your tenant, such as "products" or "avatars".
    ///
    /// A collection organises images; it does not scope their identity. An image id is unique across the
    /// whole tenant, so the same id cannot be used in two collections.
    /// </summary>
    public string? Collection { get; init; }
}
