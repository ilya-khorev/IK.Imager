using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Abstractions.Models;
using IK.Imager.Abstractions.Storage;
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
            var configuration1 = configuration;
            CloudStorageAccount storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            _cloudBlobClient = storageAccount.CreateCloudBlobClient();

            _imagesContainer = new Lazy<CloudBlobContainer>(() => CloudBlobContainer(configuration1.ImagesContainerName));
            _thumbnailsContainer = new Lazy<CloudBlobContainer>(() => CloudBlobContainer(configuration1.ThumbnailsContainerName));
        }

        private CloudBlobContainer CloudBlobContainer(string containerName)
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

        public async Task<string> UploadImage(Stream imageStream, ImageType imageType, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid().ToString();
            await UploadImage(id, imageStream, imageType, cancellationToken);
            return id;
        }

        public async Task UploadImage(string id, Stream imageStream, ImageType imageType, CancellationToken cancellationToken)
        {
            var blockBlob = GetBlockBlob(id, imageType);
            await blockBlob.UploadFromStreamAsync(imageStream, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Stream> DownloadImage(string id,  ImageType imageType, CancellationToken cancellationToken)
        {
            var blockBlob = GetBlockBlob(id, imageType);
            if (blockBlob == null)
                return null;

            MemoryStream memoryStream = new MemoryStream();
            await blockBlob.DownloadToStreamAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            return memoryStream;
        }

        public async Task<bool> TryDeleteImage(string id,  ImageType imageType, CancellationToken cancellationToken)
        {
            var blockBlob = GetBlockBlob(id, imageType);
            return await blockBlob.DeleteIfExistsAsync(cancellationToken).ConfigureAwait(false);
        }

        public Uri GetImageUri(string id, ImageType imageType)
        {
            var blockBlob = GetBlockBlob(id, imageType);
            return blockBlob.Uri;
        }

        private CloudBlockBlob GetBlockBlob(string id, ImageType imageType)
        {
            if (imageType == ImageType.Original)
                return _imagesContainer.Value.GetBlockBlobReference(id);
            
            return _thumbnailsContainer.Value.GetBlockBlobReference(id);
        }
    }
}