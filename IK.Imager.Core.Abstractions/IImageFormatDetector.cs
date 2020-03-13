using System;
using System.IO;

namespace IK.Imager.Core.Abstractions
{
    public interface IImageFormatDetector
    {
        /// <summary>
        /// Detects the image format by reading its header
        /// Returns null if the system cannot recognize the given stream as an image
        /// </summary>
        /// <param name="imageStream">Image stream</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">Thrown, when an image format is different from jpg, png, bmp, or gif</exception>
        ImageFormat DetectFormat(Stream imageStream);
    }
}