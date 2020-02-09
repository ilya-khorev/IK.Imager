using System.Collections.Generic;

namespace IK.Imager.Abstractions.Models
{
    public class ImageMetadata: IImageBasicDetails
    {
        public string Id { get; set; }
        public int Size { get; set; }
        public string MD5Hash { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
     
        /// <summary>
        /// Image name. Optional attribute
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Image format, e.g. png, jpg, ...
        /// </summary>
        public string Format { get; set; } //todo enum?
        
        /// <summary>
        /// Additional information associated with an image in arbitrary form of key-value dictionary
        /// </summary>
        public IDictionary<string, string> Tags { get; set; }
        
        /// <summary>
        /// Thumbnails of an image.
        /// Sorted by dimensions descending, so that the biggest thumbnail is the last element in the array.
        /// Optional property: sometimes an image either doesn't have any thumbnails at all or they are not prepared yet.
        /// </summary>
        public ImageThumbnail[] Thumbnails { get; set; }
    }
}