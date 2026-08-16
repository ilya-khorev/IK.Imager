using System;
using System.IO;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Abstractions.Thumbnails;
using IK.Imager.Storage.Abstractions.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace IK.Imager.Core.Thumbnails;

public class ImageResizer : IImageResizer
{
    public ImageResizeResult Resize(Stream imageStream, ImageType imageType, int targetWidth)
    {
        ArgumentNullException.ThrowIfNull(imageStream);

        imageStream.Position = 0;

        IImageEncoder imageEncoder = imageType switch
        {
            ImageType.JPEG => new JpegEncoder(),
            ImageType.BMP => new BmpEncoder(),
            ImageType.GIF => new GifEncoder(),
            _ => new PngEncoder()
        };

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
        image.Save(resultStream, imageEncoder);
        return new ImageResizeResult(resultStream, new ImageSize(image.Width, image.Height, resultStream.Length));
    }
}
