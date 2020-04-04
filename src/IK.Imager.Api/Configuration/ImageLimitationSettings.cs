namespace IK.Imager.Api.Configuration
{
    /// <summary>
    /// Image threshold values
    /// </summary>
    public class ImageLimitationSettings
    {
        /// <summary>
        /// Max and min image width in pixels 
        /// </summary>
        public Range Width { get; set; }
        
        /// <summary>
        /// Max and min image height in pixels 
        /// </summary>
        public Range Height { get; set; }
        
        /// <summary>
        /// Max and min image size in bytes
        /// </summary>
        public Range SizeBytes { get; set; }
        
        /// <summary>
        /// Supported image types (PNG, BMP, GIF, JPEG only allowed)
        /// </summary>
        public string[] Types { get; set; }
    }

    /// <summary>
    /// Type that represent a range with min and max values
    /// </summary>
    public class Range
    {
        /// <summary>
        /// Min value
        /// </summary>
        public int Min { get; set; }
        
        /// <summary>
        /// Max value
        /// </summary>
        public int Max { get; set; }
    }
}