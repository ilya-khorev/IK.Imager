namespace IK.Imager.Core.Abstractions
{
    /// <summary>
    /// Builds the blob path of an image and of its thumbnails.
    ///
    /// The path is the delivery url, so everything about it is deliberate:
    /// <c>{tenant}/[{collection}/][{prefix}/]{imageId}.{extension}</c>. The tenant has to be there because
    /// ids are only unique within one, and the two middle segments are what the caller asked for.
    /// </summary>
    public interface IImageNameGenerator
    {
        /// <summary>
        /// A random id, for an upload that did not name one. Long enough that the url it produces cannot be
        /// guessed, which is the only thing keeping a publicly readable blob private.
        /// </summary>
        string NewImageId();

        /// <summary>
        /// A random path segment that makes a url unguessable even when the id is not. This is what lets a
        /// caller keep a readable id, such as a product code, without publishing a url anyone can construct.
        /// </summary>
        string NewUniquePrefix();

        /// <summary>
        /// Assembles the blob path of an original image. A null or empty collection or prefix contributes
        /// no segment.
        /// </summary>
        string BuildBlobPath(string tenantId, string? collection, string? uniquePrefix, string imageId, string extension);

        /// <summary>
        /// Assembles the blob path of a thumbnail from the path of its original, as
        /// <c>{original without extension}_{width}.{extension}</c>.
        ///
        /// Derived rather than rebuilt so a thumbnail inherits the tenant, collection and prefix of its
        /// original without having to be told any of them - and so regenerating thumbnails overwrites the
        /// previous set in place instead of orphaning it.
        /// </summary>
        string BuildThumbnailBlobPath(string originalBlobPath, int width, string extension);
    }
}
