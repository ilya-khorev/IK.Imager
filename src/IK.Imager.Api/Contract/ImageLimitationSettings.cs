using System;
using System.Collections.Generic;

namespace IK.Imager.Api.Contract
{
    /// <summary>
    /// Image threshold values
    /// </summary>
    public class ImageLimitationSettingsRequest
    {
        /// <summary>
        /// Max and min image width in pixels 
        /// </summary>
        public Range<int> Width { get; set; }
        
        /// <summary>
        /// Max and min image height in pixels 
        /// </summary>
        public Range<int> Height { get; set; }
        
        /// <summary>
        /// Max and min image size in bytes
        /// </summary>
        public Range<int> SizeBytes { get; set; }
        
        /// <summary>
        /// Max and min image aspect ratio
        /// Aspect ratio - the ratio of its width to its height
        /// </summary>
        public Range<double> AspectRatio { get; set; }
        
        /// <summary>
        /// Supported image types (PNG, BMP, GIF, JPEG only allowed)
        /// </summary>
        public List<string> Types { get; set; }
    }

    /// <summary>
    /// Type that represent a range with min and max values
    /// </summary>
    public class Range<T> where T: IComparable<T>
    {
        /// <summary>
        /// Min value
        /// </summary>
        public T Min { get; set; }
        
        /// <summary>
        /// Max value
        /// </summary>
        public T Max { get; set; }
    }
}