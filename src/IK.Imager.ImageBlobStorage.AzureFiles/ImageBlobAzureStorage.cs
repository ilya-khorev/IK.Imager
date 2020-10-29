using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Storage;
using IK.Imager.Utils;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Options;

namespace IK.Imager.ImageBlobStorage.AzureFiles
{
    public class ImageBlobAzureStorage : IImageBlobStorage
    {
        private readonly CloudBlobClient _cloudBlobClient;

        private readonly Lazy<CloudBlobContainer> _imagesContainer;
        private readonly Lazy<CloudBlobContainer> _thumbnailsContainer;

        public ImageBlobAzureStorage(IOptions<ImageAzureStorageSettings> settings)
        {
            ArgumentHelper.AssertNotNull(nameof(settings), settings);

            var storageAccount = CloudStorageAccount.Parse(settings.Value.ConnectionString);

            _cloudBlobClient = storageAccount.CreateCloudBlobClient();

            _imagesContainer = new Lazy<CloudBlobContainer>(() => CreateCloudBlobContainer(settings.Value.ImagesContainerName.ToLowerInvariant()));
            _thumbnailsContainer = new Lazy<CloudBlobContainer>(() => CreateCloudBlobContainer(settings.Value.ThumbnailsContainerName.ToLowerInvariant()));
        }

        private CloudBlobContainer CreateCloudBlobContainer(string containerName)
        {
            var container = _cloudBlobClient.GetContainerReference(containerName);
            container.CreateIfNotExists();
            //setting up public access for this container so that it will be available by url
            container.SetPermissions(new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Blob
            });
            return container;
        }
        
        public async Task<UploadImageResult> UploadImage(string imageName, Stream imageStream, ImageSizeType imageSizeType,
            string imageContentType, CancellationToken cancellationToken)
        {
            ArgumentHelper.AssertNotNullOrEmpty(nameof(imageName), imageName);
            ArgumentHelper.AssertNotNull(nameof(imageStream), imageStream);

            var blockBlob = GetBlockBlob(imageName, imageSizeType);
            blockBlob.Properties.ContentType = imageContentType;
             
            imageStream.Position = 0;
            await blockBlob.UploadFromStreamAsync(imageStream, cancellationToken).ConfigureAwait(false);

            return new UploadImageResult
            {
                MD5Hash = blockBlob.Properties.ContentMD5,
                DateAdded = blockBlob.Properties.Created ?? DateTimeOffset.Now,
                Url = blockBlob.Uri
            };
        }

        public async Task<MemoryStream> DownloadImage(string imageName, ImageSizeType imageSizeType,
            CancellationToken cancellationToken)
        {
            ArgumentHelper.AssertNotNullOrEmpty(nameof(imageName), imageName);

            var blockBlob = GetBlockBlob(imageName, imageSizeType);
            if (blockBlob == null)
                return null;

            MemoryStream memoryStream = new MemoryStream();
            await blockBlob.DownloadToStreamAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public async Task<bool> TryDeleteImage(string imageName, ImageSizeType imageSizeType, CancellationToken cancellationToken)
        {
            ArgumentHelper.AssertNotNullOrEmpty(nameof(imageName), imageName);

            var blockBlob = GetBlockBlob(imageName, imageSizeType);
            return await blockBlob.DeleteIfExistsAsync(cancellationToken).ConfigureAwait(false);
        }

        public Uri GetImageUri(string imageName, ImageSizeType imageSizeType)
        {
            ArgumentHelper.AssertNotNullOrEmpty(nameof(imageName), imageName);

            var blockBlob = GetBlockBlob(imageName, imageSizeType);
            return blockBlob.Uri;
        }

        public async Task<bool> ImageExists(string imageName, ImageSizeType imageSizeType, CancellationToken cancellationToken)
        {
            ArgumentHelper.AssertNotNullOrEmpty(nameof(imageName), imageName);

            var blockBlob = GetBlockBlob(imageName, imageSizeType);
            return await blockBlob.ExistsAsync(cancellationToken).ConfigureAwait(false);
        }

        private CloudBlockBlob GetBlockBlob(string name, ImageSizeType imageSizeType)
        {
            if (imageSizeType == ImageSizeType.Original)
                return _imagesContainer.Value.GetBlockBlobReference(name);

            return _thumbnailsContainer.Value.GetBlockBlobReference(name);
        }
    }
}