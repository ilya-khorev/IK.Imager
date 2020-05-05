using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Models;
using Xunit;

namespace IK.Imager.Core.Tests
{
    public class ImageResizingTests
    {
        private readonly ImageResizing _imageResizing;
        
        public ImageResizingTests()
        {
            _imageResizing = new ImageResizing();
        }

        [Fact]
        public async Task JpegResizingTest()
        {
            await CheckDimensionsAfterResize(ImageTestsHelper.JpegImagesDirectory);
        }
        
        [Fact]
        public async Task PngResizingTest()
        {
            await CheckDimensionsAfterResize(ImageTestsHelper.PngImagesDirectory);
        }
        
        [Fact]
        public async Task BmpResizingTest()
        {
            await CheckDimensionsAfterResize(ImageTestsHelper.BmpImagesDirectory);
        }
        
        [Fact]
        public async Task GifResizingTest()
        {
            await CheckDimensionsAfterResize(ImageTestsHelper.GifImagesDirectory);
        }

        private async Task CheckDimensionsAfterResize(string directory)
        {
            var images = GetImagesFromDirectory(directory);

            await using var originalImageStream = ImageTestsHelper.OpenFileForReading(images[0].FilePath);
            
            for (int i = 1; i < images.Count; i++)
            {
                var imageResizingResult = _imageResizing.Resize(originalImageStream, ImageType.JPEG, images[i].Width);
                Assert.Equal(images[i].Height, imageResizingResult.Size.Height);
            }
        }

        /// <summary>
        /// Getting a list of images in a given directory and sort them from biggest to the smallest.
        /// It parses the image file names, taking into consideration the following image name format: [name]-[width]x[height].(jpg|png|bmp|gif)
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        private List<ImageInfo> GetImagesFromDirectory(string directory)
        {
            var files = Directory.GetFiles(directory);
            List<ImageInfo> result = new List<ImageInfo>(files.Length);
            
            foreach (var file in files)
            {
                var size = Regex.Match(file, "\\d+x\\d+");
 
                var sizeArray = size.Value
                    .Split("x")
                    .Select(x => Convert.ToInt32(x))
                    .ToArray();
                
                result.Add(new ImageInfo
                {
                    FilePath = file,
                    Width = sizeArray[0],
                    Height = sizeArray[1]
                });
            }

            return result.OrderByDescending(x => x.Height + x.Width).ToList();
        }
        
        class ImageInfo
        {
            public string FilePath { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }
    }
}