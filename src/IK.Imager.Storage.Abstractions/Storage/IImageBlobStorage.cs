using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;

namespace IK.Imager.Storage.Abstractions.Storage
{
    /// <summary>
    /// Represents a set of methods for
    /// 1) uploading and storing a new image
    /// 2) downloading the previously saved images
    /// 3) removing the previously saved images
    /// </summary>
    public interface IImageBlobStorage
    {
        /// <summary>
        /// Upload and save a new image
        /// </summary>
        /// <param name="imageName">Unique image image (with or without extension)</param>
        /// <param name="imageStream">Image stream</param>
        /// <param name="imageSizeType">Original or thumbnail</param>
        /// <param name="contentType">Image content type (e.g. jpeg)</param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        /// <returns></returns>
        Task<UploadImageResult> UploadImage(string imageName, Stream imageStream, ImageSizeType imageSizeType, string contentType, CancellationToken cancellationToken);

        /// <summary>
        /// Download an image stream for a given image id
        /// </summary>
        /// <param name="imageName">Unique image image (with or without extension)</param>
        /// <param name="imageSizeType">Original or thumbnail</param>
         /// <param name="cancellationToken">Cancellation token to stop operation</param>
        /// <returns>Image stream, or null if such image was not found</returns>
        Task<MemoryStream> DownloadImage(string imageName, ImageSizeType imageSizeType, CancellationToken cancellationToken);

        /// <summary>
        /// Attempt to delete an image by a given image id 
        /// </summary>
        /// <param name="imageName">Unique image image (with or without extension)</param>
        /// <param name="imageSizeType">Original or thumbnail</param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        /// <returns>Returns true if an image was found and removed.
        /// Returns false if an image was not found.</returns>
        Task<bool> TryDeleteImage(string imageName, ImageSizeType imageSizeType, CancellationToken cancellationToken);

        /// <summary>
        /// Returns image URI by a given image id
        /// </summary>
        /// <param name="imageName">Unique image image (with or without extension)</param>
        /// <param name="imageSizeType">Original or thumbnail</param>
        /// <returns></returns>
        Uri GetImageUri(string imageName, ImageSizeType imageSizeType);

        /// <summary>
        /// Checks if a given image exists
        /// </summary>
        /// <param name="imageName">Unique image image (with or without extension)</param>
        /// <param name="imageSizeType">Original or thumbnail</param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        /// <returns>Returns true if an image was found. Otherwise, returns false.</returns>
        Task<bool> ImageExists(string imageName, ImageSizeType imageSizeType, CancellationToken cancellationToken);
    }
}