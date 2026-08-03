using System.Collections.Generic;

namespace IK.Imager.Core.Abstractions.Models
{
    public class ImagesSearchResult
    {
        /// <summary>
        /// Set of images
        /// </summary>
        public List<ImageFullInfoWithThumbnails> Images { get; set; } = new();
    }
    
    /// <summary>
    /// Model containing information about image and its thumbnails
    /// </summary>
    public class ImageFullInfoWithThumbnails: ImageInfo
    {
        /// <summary>
        /// Additional information associated with an image in arbitrary form of key-value dictionary.
        /// Optional: an image may carry no tags at all.
        /// </summary>
        public IDictionary<string, string>? Tags { get; set; }

        /// <summary>
        /// Image thumbnails sorted by smallest to the biggest.
        /// Empty while thumbnail generation is still in flight.
        /// </summary>
        public List<ImageInfo> Thumbnails { get; set; } = new();
    }
}