using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;

namespace IK.Imager.Storage.Abstractions.Repositories
{
    /// <summary>
    /// Represents a set of methods for
    /// 1) uploading and storing a new image
    /// 2) downloading the previously saved images
    /// 3) removing the previously saved images
    ///
    /// ImageVariant is required for most of operations.
    /// So, it's recommended to store different image types (e.g. original, or thumbnails) in different places (e.g. folders, or containers)
    /// </summary>
    public interface IImageBlobRepository
    {
        /// <summary>
        /// Uploads and saves a new image in the storage
        /// </summary>
        /// <param name="blobPath">Path of the blob within its container, extension included</param>
        /// <param name="imageStream">Image stream</param>
        /// <param name="variant">Original or thumbnail</param>
        /// <param name="contentType">Image content type (e.g. jpeg, png)</param>
        /// <param name="allowOverwrite">
        /// Whether an existing blob at that path may be replaced. False for an original, whose path is the
        /// caller's to choose and must not silently overwrite someone else's image. True for a thumbnail,
        /// whose path is derived from its original and is therefore regenerated in place - a redelivered
        /// thumbnail job would otherwise fail forever.
        /// </param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        /// <exception cref="BlobAlreadyExistsException">
        /// A blob already exists at that path and <paramref name="allowOverwrite"/> is false.
        /// </exception>
        Task<BlobUploadResult> UploadImage(string blobPath, Stream imageStream, ImageVariant variant, string contentType, bool allowOverwrite, CancellationToken cancellationToken);

        /// <summary>
        /// Downloads an image stream for a given blob path
        /// </summary>
        /// <param name="blobPath">Path of the blob within its container, extension included</param>
        /// <param name="variant">Original or thumbnail</param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        /// <returns>Image stream, or null if such image was not found</returns>
        Task<MemoryStream?> DownloadImage(string blobPath, ImageVariant variant, CancellationToken cancellationToken);

        /// <summary>
        /// Attempts to delete an image by a given blob path
        /// </summary>
        /// <param name="blobPath">Path of the blob within its container, extension included</param>
        /// <param name="variant">Original or thumbnail</param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        /// <returns>Returns true if an image was found and removed.
        /// Returns false if an image was not found.</returns>
        Task<bool> TryDeleteImage(string blobPath, ImageVariant variant, CancellationToken cancellationToken);

        /// <summary>
        /// Returns an image URI by a given blob path
        /// </summary>
        /// <param name="blobPath">Path of the blob within its container, extension included</param>
        /// <param name="variant">Original or thumbnail</param>
        /// <returns></returns>
        Uri GetImageUri(string blobPath, ImageVariant variant);

        /// <summary>
        /// Checks if a given image exists
        /// </summary>
        /// <param name="blobPath">Path of the blob within its container, extension included</param>
        /// <param name="variant">Original or thumbnail</param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        /// <returns>Returns true if an image was found. Otherwise, returns false.</returns>
        Task<bool> ImageExists(string blobPath, ImageVariant variant, CancellationToken cancellationToken);
    }
}
