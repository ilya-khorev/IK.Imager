using System.IO;

namespace IK.Imager.Core.Abstractions
{
    public interface IImageMetadataReader
    {
        /// <summary>
        /// Detects the image format by reading its header
        /// Returns null if the system cannot recognize the given stream as an image, or the image format is different from jpg, png, bmp, or gif.
        /// </summary>
        /// <param name="imageStream">Image stream</param>
        /// <returns></returns>
        ImageFormat DetectFormat(Stream imageStream);

        /// <summary>
        /// Read image size and resolution by reading its header
        /// Returns null if the system cannot recognize the given stream as an image
        /// </summary>
        /// <param name="imageStream">Image stream</param>
        /// <returns></returns>
        ImageSize ReadSize(Stream imageStream);
    }
}