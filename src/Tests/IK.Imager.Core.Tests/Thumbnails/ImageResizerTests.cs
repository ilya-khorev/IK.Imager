using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IK.Imager.Core.Tests.Infrastructure;
using IK.Imager.Core.Thumbnails;
using IK.Imager.Storage.Abstractions.Models;
using Xunit;

namespace IK.Imager.Core.Tests.Thumbnails
{
    public class ImageResizerTests
    {
        private readonly ImageResizer _imageResizer;

        public ImageResizerTests()
        {
            _imageResizer = new ImageResizer();
        }

        [Fact]
        public async Task Resize_Jpeg_ReturnsExpectedDimensions()
        {
            await CheckDimensionsAfterResize(SampleImages.JpegImagesDirectory);
        }

        [Fact]
        public async Task Resize_Png_ReturnsExpectedDimensions()
        {
            await CheckDimensionsAfterResize(SampleImages.PngImagesDirectory);
        }

        [Fact]
        public async Task Resize_Bmp_ReturnsExpectedDimensions()
        {
            await CheckDimensionsAfterResize(SampleImages.BmpImagesDirectory);
        }

        [Fact]
        public async Task Resize_Gif_ReturnsExpectedDimensions()
        {
            await CheckDimensionsAfterResize(SampleImages.GifImagesDirectory);
        }

        private async Task CheckDimensionsAfterResize(string directory)
        {
            var images = GetImagesFromDirectory(directory);

            await using var originalImageStream = SampleImages.OpenFileForReading(images[0].FilePath);

            for (int i = 1; i < images.Count; i++)
            {
                var resizeResult = _imageResizer.Resize(originalImageStream, ImageType.JPEG, images[i].Width);
                Assert.Equal(images[i].Height, resizeResult.Size.Height);
                Assert.Equal(images[i].Width, resizeResult.Size.Width);
                Assert.True(originalImageStream.Length > resizeResult.Size.Bytes);
            }
        }

        /// <summary>
        /// Getting a list of images in a given directory and sort them from biggest to the smallest.
        /// It parses the image file names, taking into consideration the following image name format: [name]-[width]x[height].(jpg|png|bmp|gif)
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        private List<SampleImageFile> GetImagesFromDirectory(string directory)
        {
            var files = Directory.GetFiles(directory);
            List<SampleImageFile> result = new List<SampleImageFile>(files.Length);

            foreach (var file in files)
            {
                var size = Regex.Match(file, "\\d+x\\d+");

                var sizeArray = size.Value
                    .Split("x")
                    .Select(x => Convert.ToInt32(x))
                    .ToArray();

                result.Add(new SampleImageFile
                {
                    FilePath = file,
                    Width = sizeArray[0],
                    Height = sizeArray[1]
                });
            }

            return result.OrderByDescending(x => x.Height + x.Width).ToList();
        }

        class SampleImageFile
        {
            public string FilePath { get; set; } = null!;
            public int Width { get; set; }
            public int Height { get; set; }
        }
    }
}
