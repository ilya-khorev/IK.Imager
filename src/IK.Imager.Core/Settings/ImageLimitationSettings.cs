using System;
using System.Collections.Generic;
using System.Linq;

namespace IK.Imager.Core.Settings
{
    /// <summary>
    /// Image threshold values
    /// </summary>
    public class ImageLimitationSettings
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
        /// Supported image types 
        /// </summary>
        public List<string> Types { get; set; }

        public void MergeWith(ImageLimitationSettings settings)
        {
            if (settings == null)
                return;

            if (settings.Height != null)
            {
                Height = new Range<int>
                {
                    Min = settings.Height.Min,
                    Max = settings.Height.Max
                };
            }

            if (settings.Width != null)
            {
                Width = new Range<int>
                {
                    Min = settings.Width.Min,
                    Max = settings.Width.Max
                };
            }

            if (settings.SizeBytes != null)
            {
                SizeBytes = new Range<int>
                {
                    Min = settings.SizeBytes.Min,
                    Max = settings.SizeBytes.Max
                };
            }
            
            if (settings.AspectRatio != null)
            {
                AspectRatio = new Range<double>
                {
                    Min = settings.AspectRatio.Min,
                    Max = settings.AspectRatio.Max
                };
            }

            if (settings.Types != null) 
                Types = settings.Types.ToList();
        }
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