using System;

namespace IK.Imager.Api.Contract
{
    /// <summary>
    /// Model containing information about image
    /// </summary>
    public class ImageInfo
    {
        /// <summary>
        /// Unique image identifier
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Image https url
        /// </summary>
        public string Url { get; set; }
        
        /// <summary>
        /// Image hashcode
        /// </summary>
        public string Hash { get; set; }
        
        /// <summary>
        /// Timestamp when the image has been saved in the system
        /// </summary>
        public DateTimeOffset DateAdded { get; set; }
        
        /// <summary>
        /// Image width in pixels
        /// </summary>
        public int Width { get; set; }
        
        /// <summary>
        /// Image height in pixels
        /// </summary>
        public int Height { get; set; }
        
        /// <summary>
        /// Image size in bytes
        /// </summary>
        public long Bytes { get; set; }
        
        /// <summary>
        /// Standard that indicates the nature and format of a file.
        /// E.g. 'image/jpeg', 'image/png', 'image/bmp', 'image/gif'
        /// </summary>
        public string MimeType { get; set;}
    }
}