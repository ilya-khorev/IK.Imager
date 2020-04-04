using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using IK.Imager.Core.Abstractions;
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
        public void JpegResizingTest()
        {
            ResizingTest(ImageTestsHelper.JpegImagesDirectory);
        }
        
        [Fact]
        public void PngResizingTest()
        {
            ResizingTest(ImageTestsHelper.PngImagesDirectory);
        }
        
        [Fact]
        public void BmpResizingTest()
        {
            ResizingTest(ImageTestsHelper.BmpImagesDirectory);
        }
        
        [Fact]
        public void GifResizingTest()
        {
            ResizingTest(ImageTestsHelper.GifImagesDirectory);
        }

        private void ResizingTest(string directory)
        {
            var images = SelectImages(directory);

            var originalImageStream = ImageTestsHelper.OpenFileForReading(images[0].FilePath);
            
            for (int i = 1; i < images.Count; i++)
            {
                var imageResizingResult = _imageResizing.Resize(originalImageStream, ImageType.JPEG, images[i].Width);
                Assert.Equal(images[i].Height, imageResizingResult.Size.Height);
            }
        }

        /// <summary>
        /// Getting a list of images in a given directory and sort them from biggest to the smallest
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        private List<ImageInfo> SelectImages(string directory)
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