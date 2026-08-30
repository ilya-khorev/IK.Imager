using System;

namespace IK.Imager.Core.Abstractions.Models
{
    /// <summary>
    /// Model containing information about image
    /// </summary>
    public record ImageDetails
    {
        /// <summary>
        /// Unique image identifier
        /// </summary>
        public string Id { get; init; } = null!;

        /// <summary>
        /// Path of the image blob within its container, extension included.
        /// </summary>
        public string BlobPath { get; init; } = null!;

        /// <summary>
        /// Collection this image was uploaded into, if any.
        /// </summary>
        public string? Collection { get; init; }

        /// <summary>
        /// Image https url
        /// </summary>
        public Uri Url { get; init; } = null!;

        /// <summary>
        /// Image hashcode
        /// </summary>
        public string Hash { get; init; } = null!;

        /// <summary>
        /// Timestamp when the image has been saved in the system
        /// </summary>
        public DateTimeOffset DateAdded { get; init; }

        /// <summary>
        /// Image width in pixels
        /// </summary>
        public int Width { get; init; }

        /// <summary>
        /// Image height in pixels
        /// </summary>
        public int Height { get; init; }

        /// <summary>
        /// Image size in bytes
        /// </summary>
        public long Bytes { get; init; }

        /// <summary>
        /// Standard that indicates the nature and format of a file.
        /// E.g. 'image/jpeg', 'image/png', 'image/bmp', 'image/gif'
        /// </summary>
        public string MimeType { get; init; } = null!;
    }
}
