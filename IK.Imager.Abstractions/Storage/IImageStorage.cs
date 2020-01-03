﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Abstractions.Models;

namespace IK.Imager.Abstractions.Storage
{
    /// <summary>
    /// Represents a set of methods for
    /// 1) uploading and storing a new image
    /// 2) downloading the previously saved images
    /// 3) removing the previously saved images
    /// </summary>
    public interface IImageStorage
    {
        /// <summary>
        /// Upload and save a new image.
        /// Image identifier is generated and returned as a result of this method.
        /// </summary>
        /// <param name="imageStream">Image stream</param>
        /// <param name="imageType">Original or thumbnail</param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        /// <returns>Image identifier</returns>
        Task<string> UploadImage(Stream imageStream, ImageType imageType, CancellationToken cancellationToken);

        /// <summary>
        /// Upload and save a new image
        /// </summary>
        /// <param name="id">Unique identifier of an image</param>
        /// <param name="imageStream">Image stream</param>
        /// <param name="imageType">Original or thumbnail</param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        /// <returns></returns>
        Task UploadImage(string id, Stream imageStream, ImageType imageType, CancellationToken cancellationToken);

        /// <summary>
        /// Download an image stream for a given image id
        /// </summary>
        /// <param name="id">Image identifier</param>
        /// <param name="imageType">Original or thumbnail</param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        /// <returns>Image stream, or null if such image was not found</returns>
        Task<Stream> DownloadImage(string id, ImageType imageType, CancellationToken cancellationToken);

        /// <summary>
        /// Attempt to delete an image by a given image id 
        /// </summary>
        /// <param name="id">Image identifier</param>
        /// <param name="imageType">Original or thumbnail</param>
        /// <param name="cancellationToken">cancellation token to stop operation</param>
        /// <returns>Returns true if an image was found and removed.
        /// Returns false if an image was not found.</returns>
        Task<bool> TryDeleteImage(string id, ImageType imageType, CancellationToken cancellationToken);

        /// <summary>
        /// Returns image URI by a given image id
        /// </summary>
        /// <param name="id">Image identifier</param>
        /// <param name="imageType">Original or thumbnail</param>
        /// <returns></returns>
        Uri GetImageUri(string id, ImageType imageType);
    }
}