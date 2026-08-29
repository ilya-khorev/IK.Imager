namespace IK.Imager.Api.Contract.Upload;

/// <summary>
/// Model that represent a request for uploading a new image
/// </summary>
public abstract record UploadImageRequestBase
{
    /// <summary>
    /// Id of the new image, which also forms its url.
    ///
    /// Optional - a random id is generated when it is omitted. An id must be unique within your tenant, and
    /// uploading one that is already taken is rejected with a 409; replace an image by deleting it first.
    /// Lowercase letters and digits, with dots, underscores and hyphens allowed between them.
    ///
    /// Note the url also carries the file extension, which the service determines from the image itself, so
    /// read the returned url rather than assembling it from the id.
    /// </summary>
    public string? ImageId { get; init; }

    /// <summary>
    /// Optional label grouping images within your tenant, such as "products" or "avatars".
    ///
    /// A collection organises images; it does not scope their identity. An image id is unique across the
    /// whole tenant, so the same id cannot be used in two collections.
    /// </summary>
    public string? Collection { get; init; }

    /// <summary>
    /// Whether the collection also becomes a segment of the image url.
    /// Requires a collection to be given. False by default, which keeps the collection out of the url.
    /// </summary>
    public bool IncludeCollectionInPath { get; init; }

    /// <summary>
    /// Whether a random segment is inserted into the url before the image id.
    ///
    /// False by default, so the url is exactly what the id predicts. Turn it on to keep a readable id while
    /// making the url unguessable - images are served publicly by url, so an id anyone can guess is a url
    /// anyone can open. It does not affect uniqueness: an id already in use is still rejected.
    /// </summary>
    public bool AddUniquePrefix { get; init; }
}
