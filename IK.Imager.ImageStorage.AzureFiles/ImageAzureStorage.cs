using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Storage;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace IK.Imager.ImageStorage.AzureFiles
{
    public class ImageAzureStorage : IImageStorage
    {
        private readonly CloudBlobClient _cloudBlobClient;

        private readonly Lazy<CloudBlobContainer> _imagesContainer;
        private readonly Lazy<CloudBlobContainer> _thumbnailsContainer;
        
        public ImageAzureStorage(ImageAzureStorageConfiguration configuration)
        {
            ArgumentHelper.AssertNotNull(nameof(configuration), configuration);
            
            var storageAccount = string.IsNullOrWhiteSpace(configuration.ConnectionString)
                ? CloudStorageAccount.DevelopmentStorageAccount
                : CloudStorageAccount.Parse(configuration.ConnectionString);

            _cloudBlobClient = storageAccount.CreateCloudBlobClient();

            _imagesContainer =
                new Lazy<CloudBlobContainer>(() => CreateCloudBlobContainer(configuration.ImagesContainerName));
            _thumbnailsContainer =
                new Lazy<CloudBlobContainer>(() => CreateCloudBlobContainer(configuration.ThumbnailsContainerName));
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

        public async Task<UploadImageResult> UploadImage(Stream imageStream, ImageType imageType, 
            string imageContentType, CancellationToken cancellationToken)
        {
            ArgumentHelper.AssertNotNull(nameof(imageStream), imageStream);

            var id = Guid.NewGuid().ToString();
            return await UploadImage(id, imageStream, imageType, imageContentType, cancellationToken);
        }

        public async Task<UploadImageResult> UploadImage(string id, Stream imageStream, ImageType imageType,
            string imageContentType, CancellationToken cancellationToken)
        {
            ArgumentHelper.AssertNotNullOrEmpty(nameof(id), id);
            ArgumentHelper.AssertNotNull(nameof(imageStream), imageStream);

            var blockBlob = GetBlockBlob(id, imageType);
            blockBlob.Properties.ContentType = imageContentType;
            await blockBlob.UploadFromStreamAsync(imageStream, cancellationToken).ConfigureAwait(false);

            return new UploadImageResult
            {
                Id = id,
                MD5Hash = blockBlob.Properties.ContentMD5,
                DateAdded = blockBlob.Properties.Created ?? DateTimeOffset.Now
            };
        }

        public async Task<MemoryStream> DownloadImage(string id, ImageType imageType, CancellationToken cancellationToken)
        {
            ArgumentHelper.AssertNotNullOrEmpty(nameof(id), id);

            var blockBlob = GetBlockBlob(id, imageType);
            if (blockBlob == null)
                return null;

            MemoryStream memoryStream = new MemoryStream();
            await blockBlob.DownloadToStreamAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public async Task<bool> TryDeleteImage(string id, ImageType imageType, CancellationToken cancellationToken)
        {
            ArgumentHelper.AssertNotNullOrEmpty(nameof(id), id);

            var blockBlob = GetBlockBlob(id, imageType);
            return await blockBlob.DeleteIfExistsAsync(cancellationToken).ConfigureAwait(false);
        }

        public Uri GetImageUri(string id, ImageType imageType)
        {
            ArgumentHelper.AssertNotNullOrEmpty(nameof(id), id);

            var blockBlob = GetBlockBlob(id, imageType);
            return blockBlob.Uri;
        }

        public async Task<bool> ImageExists(string id, ImageType imageType, CancellationToken cancellationToken)
        {
            ArgumentHelper.AssertNotNullOrEmpty(nameof(id), id);
            
            var blockBlob = GetBlockBlob(id, imageType);
            return await blockBlob.ExistsAsync(cancellationToken).ConfigureAwait(false);
        }

        private CloudBlockBlob GetBlockBlob(string id, ImageType imageType)
        {
            if (imageType == ImageType.Original)
                return _imagesContainer.Value.GetBlockBlobReference(id);

            return _thumbnailsContainer.Value.GetBlockBlobReference(id);
        }
    }
}