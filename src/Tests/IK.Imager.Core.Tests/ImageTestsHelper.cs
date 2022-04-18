using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.ImageUploading;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using ImageType = IK.Imager.Storage.Abstractions.Models.ImageType;

namespace IK.Imager.Core.Tests
{
    public static class ImageTestsHelper
    {
        public const string ImagesDirectory = "Images";
        public const string JpegImagesDirectory = ImagesDirectory + "\\jpeg";
        public const string PngImagesDirectory = ImagesDirectory + "\\png";
        public const string BmpImagesDirectory = ImagesDirectory + "\\bmp";
        public const string GifImagesDirectory = ImagesDirectory + "\\gif";

        public const string WebpImagePath = ImagesDirectory + "\\556-200x300.webp";
        public const string TgaImagePath = ImagesDirectory + "\\sample_640×426.tga";

        public const string TextFilePath = "textFile.txt";

        static readonly Random Random = new ();

        private static readonly string[] ImageDirectories = 
        {
            JpegImagesDirectory, PngImagesDirectory, BmpImagesDirectory, GifImagesDirectory
        };

        public static FileStream OpenFileForReading(string filePath)
        {
            return File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        
        public static async Task<ImageMetadata> UploadImage(IImageBlobRepository blobRepository, IImageMetadataRepository metadataRepository, string imagePath, int width, int height, 
            string contentType, ImageType imageType, string imageGroup)
        {
            string extension = "";
            switch (imageType)
            {
                case ImageType.BMP:
                    extension = ".bmp";
                    break;
                
                case ImageType.JPEG:
                    extension = ".jpeg";
                    break;
                
                case ImageType.PNG:
                    extension = ".png";
                    break;
                
                case ImageType.GIF:
                    extension = ".gif";
                    break;
            }
            
            await using FileStream file = OpenFileForReading(imagePath);
            string imageId = Guid.NewGuid().ToString();
            string imageName = imageId + extension;
            var uploadImageResult = await blobRepository.UploadImage(imageName, file, ImageSizeType.Original, contentType, CancellationToken.None);

            var imageMetadata = new ImageMetadata
            {
                Id = imageId,
                Name = imageName,
                DateAddedUtc = uploadImageResult.DateAdded.DateTime,
                Height = height,
                Width = width,
                MD5Hash = uploadImageResult.Hash,
                SizeBytes = file.Length,
                MimeType = contentType,
                ImageType = imageType,
                ImageGroup = imageGroup 
            };

            await metadataRepository.SetMetadata(imageMetadata, CancellationToken.None);
            return imageMetadata;
        }
        
        public static async Task<ImageInfo> UploadRandomlySelectedImage(string imageGroup, ImageUploadService imageUploadService)
        { 
            var randomlySelectedImageDirectory = ImageDirectories[Random.Next(0, ImageDirectories.Length - 1)];
            var files = Directory.GetFiles(randomlySelectedImageDirectory);
            var randomlySelectedImageFile = files[Random.Next(0, files.Length - 1)];
            
            await using FileStream file = OpenFileForReading(randomlySelectedImageFile);
            return await imageUploadService.UploadImage(file, imageGroup);
        }
    }
}