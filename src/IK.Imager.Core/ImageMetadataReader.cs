using System;
using System.IO;
using System.Linq;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;

namespace IK.Imager.Core
{
    public class ImageMetadataReader: IImageMetadataReader    
    {
        public ImageFormat DetectFormat(Stream imageStream)
        {
            ArgumentHelper.AssertNotNull(nameof(imageStream), imageStream);

            imageStream.Position = 0;
            
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
            else if (imageFormat is TiffFormat)
                imageType = ImageType.TIFF;
            else if (imageFormat is WebpFormat)
                imageType = ImageType.WEBP;
            else 
                throw new NotSupportedException($"Image format {imageFormat.Name} is not supported");

            return new ImageFormat(imageFormat.DefaultMimeType, imageFormat.FileExtensions.First(), imageType);
        }

        public ImageSize ReadSize(Stream imageStream)
        {
            ArgumentHelper.AssertNotNull(nameof(imageStream), imageStream);

            imageStream.Position = 0;
            
            var identify = Image.Identify(imageStream);
            if (identify == null)
                return null;
            
            return new ImageSize
            {
                Bytes = imageStream.Length,
                Width = identify.Width,
                Height = identify.Height
            };
        }
    }
}