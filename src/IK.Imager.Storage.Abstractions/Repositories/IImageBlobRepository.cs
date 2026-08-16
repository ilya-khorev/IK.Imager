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
        /// <param name="imageName">Unique image name (with or without extension)</param>
        /// <param name="imageStream">Image stream</param>
        /// <param name="variant">Original or thumbnail</param>
        /// <param name="contentType">Image content type (e.g. jpeg, png)</param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        /// <returns></returns>
        Task<BlobUploadResult> UploadImage(string imageName, Stream imageStream, ImageVariant variant, string contentType, CancellationToken cancellationToken);

        /// <summary>
        /// Downloads an image stream for a given image name
        /// </summary>
        /// <param name="imageName">Unique image name (with or without extension)</param>
        /// <param name="variant">Original or thumbnail</param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        /// <returns>Image stream, or null if such image was not found</returns>
        Task<MemoryStream?> DownloadImage(string imageName, ImageVariant variant, CancellationToken cancellationToken);

        /// <summary>
        /// Attempts to delete an image by a given image name
        /// </summary>
        /// <param name="imageName">Unique image name (with or without extension)</param>
        /// <param name="variant">Original or thumbnail</param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        /// <returns>Returns true if an image was found and removed.
        /// Returns false if an image was not found.</returns>
        Task<bool> TryDeleteImage(string imageName, ImageVariant variant, CancellationToken cancellationToken);

        /// <summary>
        /// Returns an image URI by a given image name
        /// </summary>
        /// <param name="imageName">Unique image name (with or without extension)</param>
        /// <param name="variant">Original or thumbnail</param>
        /// <returns></returns>
        Uri GetImageUri(string imageName, ImageVariant variant);

        /// <summary>
        /// Checks if a given image exists
        /// </summary>
        /// <param name="imageName">Unique image name (with or without extension)</param>
        /// <param name="variant">Original or thumbnail</param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        /// <returns>Returns true if an image was found. Otherwise, returns false.</returns>
        Task<bool> ImageExists(string imageName, ImageVariant variant, CancellationToken cancellationToken);
    }
}
