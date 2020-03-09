using System;
using System.IO;
using IK.Imager.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace IK.Imager.Core
{
    public class ImageResizing
    {
        public MemoryStream Resize(Stream imageStream, ImageType imageType, int targetWidth)
        {
            ArgumentHelper.AssertNotNull(nameof(imageStream), imageStream);
            
            IImageFormat imageFormat = PngFormat.Instance;
            switch (imageType)
            {
                case ImageType.PNG:
                    imageFormat = PngFormat.Instance;
                    break;
                
                case ImageType.JPEG:
                    imageFormat = JpegFormat.Instance;
                    break;
                
                case ImageType.BMP:
                    imageFormat = BmpFormat.Instance;
                    break;
                
                case ImageType.GIF:
                    imageFormat = GifFormat.Instance;
                    break;
            }
            
            using var image = Image.Load(imageStream);
                
            decimal divisor = (decimal)image.Width / targetWidth;
            var targetHeight = Convert.ToInt32(Math.Round(image.Height / divisor));

            image.Mutate(x => x
                .Resize(new ResizeOptions
                {
                    Size = new Size(targetWidth, targetHeight),
                    Mode = ResizeMode.Max
                }));
            
            MemoryStream resultStream = new MemoryStream();
            image.Save(resultStream, imageFormat);
            return resultStream;
        }
    }
}