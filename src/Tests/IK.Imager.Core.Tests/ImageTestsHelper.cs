using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Storage;

namespace IK.Imager.Core.Tests
{
    public static class ImageTestsHelper
    {
        public const string JpegImagesDirectory = "Images\\jpeg";
        public const string PngImagesDirectory = "Images\\png";
        public const string BmpImagesDirectory = "Images\\bmp";
        public const string GifImagesDirectory = "Images\\gif";
        
        public static FileStream OpenFileForReading(string filePath)
        {
            return File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        
        public static async Task<ImageMetadata> UploadImage(IImageBlobStorage blobStorage, IImageMetadataStorage metadataStorage, string imagePath, int width, int height, 
            string contentType, ImageType imageType, string partitionKey)
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
            var uploadImageResult = await blobStorage.UploadImage(imageName, file, ImageSizeType.Original, contentType, CancellationToken.None);

            var imageMetadata = new ImageMetadata
            {
                Id = imageId,
                Name = imageName,
                DateAddedUtc = uploadImageResult.DateAdded.DateTime,
                Height = height,
                Width = width,
                MD5Hash = uploadImageResult.MD5Hash,
                SizeBytes = file.Length,
                MimeType = contentType,
                ImageType = imageType,
                PartitionKey = partitionKey 
            };

            await metadataStorage.SetMetadata(imageMetadata, CancellationToken.None);
            return imageMetadata;
        }
    }
}