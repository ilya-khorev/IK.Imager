using System;
using System.IO;
using System.Linq;
using IK.Imager.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace IK.Imager.Core
{
    public class ImageFormatDetector    
    {
        /// <summary>
        /// Detects the image format by reading its header
        /// Returns null if it is not an image
        /// </summary>
        /// <param name="imageStream"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">Image format is different from jpg, png, bmp, or gif</exception>
        public ImageFormat DetectFormat(Stream imageStream)
        {
            ArgumentHelper.AssertNotNull(nameof(imageStream), imageStream);
            
            var imageFormat = Image.DetectFormat(imageStream);
            if (imageFormat == null)
                return null;

            ImageType imageType;
            
            if (imageFormat is PngFormat)
                imageType = ImageType.PNG;
            else if (imageFormat is JpegFormat)
                imageType = ImageType.JPEG;
            else if (imageFormat is GifFormat)
                imageType = ImageType.GIF;
            else if (imageFormat is BmpFormat)
                imageType = ImageType.BMP;
            else 
                throw new NotSupportedException($"Image format {imageFormat.Name} is not supported");

            return new ImageFormat(imageFormat.DefaultMimeType, imageFormat.FileExtensions.First(), imageType);
        }
    }
}