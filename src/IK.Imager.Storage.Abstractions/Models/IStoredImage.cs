using System;

namespace IK.Imager.Storage.Abstractions.Models
{
    public interface IStoredImage
    {
        /// <summary>
        /// Unique identifier of an image
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Path of the image blob within its storage container, extension included.
        /// This is the storage key, not a display name.
        /// </summary>
        string BlobPath { get; set; }

        /// <summary>
        /// Image size in bytes
        /// </summary>
        long SizeBytes { get; set; }

        /// <summary>
        /// MD5 hash of an image
        /// </summary>
        string MD5Hash { get; set; }

        /// <summary>
        /// Image width in pixels
        /// </summary>
        int Width { get; set; }

        /// <summary>
        /// Image height in pixels
        /// </summary>
        int Height { get; set; }

        /// <summary>
        /// Date when an image was added to storage
        /// </summary>
        DateTime DateAddedUtc { get; set; }

        /// <summary>
        /// Standard that indicates the nature and format of a file.
        /// E.g. 'image/jpeg', 'image/png', 'image/bmp', 'image/gif'
        /// </summary>
        string MimeType { get; set; }
    }
}
