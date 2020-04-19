using System.IO;
using IK.Imager.Core.Abstractions.Models;

namespace IK.Imager.Core.Abstractions
{
    public interface IImageResizing
    {
        /// <summary>
        /// Resize a given image stream to the target width and height, corresponding to the original
        /// image aspect ratio
        /// </summary>
        /// <param name="imageStream">Image stream</param>
        /// <param name="imageType">Image type</param>
        /// <param name="targetWidth">Image target width in pixels</param>
        /// <returns></returns>
        ImageResizingResult Resize(Stream imageStream, ImageType imageType, int targetWidth);
    }
}