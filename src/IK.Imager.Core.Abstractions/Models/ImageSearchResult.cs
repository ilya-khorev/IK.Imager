using System.Collections.Generic;

namespace IK.Imager.Core.Abstractions.Models
{
    public class ImagesSearchResult
    {
        /// <summary>
        /// Set of images
        /// </summary>
        public List<ImageFullInfoWithThumbnails> Images { get; set; }
    }
    
    /// <summary>
    /// Model containing information about image and its thumbnails
    /// </summary>
    public class ImageFullInfoWithThumbnails: ImageInfo
    {
        /// <summary>
        /// Additional information associated with an image in arbitrary form of key-value dictionary
        /// </summary>
        public IDictionary<string, string> Tags { get; set; }
        
        /// <summary>
        /// Image thumbnails sorted by smallest to the biggest
        /// </summary>
        public List<ImageInfo> Thumbnails { get; set; } 
    }
}