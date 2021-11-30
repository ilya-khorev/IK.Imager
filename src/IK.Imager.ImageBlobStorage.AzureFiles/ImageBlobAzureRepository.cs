using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using IK.Imager.Utils;
using Microsoft.Extensions.Options;

namespace IK.Imager.ImageBlobStorage.AzureFiles
{
    public class ImageBlobAzureRepository : IImageBlobRepository
    {
        private readonly Lazy<BlobContainerClient> _imagesContainer;
        private readonly Lazy<BlobContainerClient> _thumbnailsContainer;

        public ImageBlobAzureRepository(IOptions<ImageAzureStorageSettings> settings, IAzureBlobClient blobClient)
        {
            ArgumentHelper.AssertNotNull(nameof(settings), settings);
            
            _imagesContainer = new Lazy<BlobContainerClient>(() => blobClient.CreateContainerIfNotExists(settings.Value.ImagesContainerName.ToLowerInvariant()));
            _thumbnailsContainer = new Lazy<BlobContainerClient>(() => blobClient.CreateContainerIfNotExists(settings.Value.ThumbnailsContainerName.ToLowerInvariant()));
        }
        
        /// <inheritdoc />
        public async Task<UploadImageResult> UploadImage(string imageName, Stream imageStream, ImageSizeType imageSizeType,
            string imageContentType, CancellationToken cancellationToken)
        {
            ArgumentHelper.AssertNotNullOrEmpty(nameof(imageName), imageName);
            ArgumentHelper.AssertNotNull(nameof(imageStream), imageStream);

            var blobClient = GetBlobClient(imageName, imageSizeType);
            //blobClient.Properties.ContentType = imageContentType;
             
            imageStream.Position = 0;
            var uploadResult = await blobClient.UploadAsync(imageStream, cancellationToken).ConfigureAwait(false);
            
            return new UploadImageResult
            {
                Hash = Convert.ToBase64String(uploadResult.Value.ContentHash),
                DateAdded = uploadResult.Value.LastModified,
                Url = blobClient.Uri
            };
        }

        /// <inheritdoc />
        public async Task<MemoryStream> DownloadImage(string imageName, ImageSizeType imageSizeType,
            CancellationToken cancellationToken)
        {
            ArgumentHelper.AssertNotNullOrEmpty(nameof(imageName), imageName);

            var blockBlob = GetBlobClient(imageName, imageSizeType);
            if (blockBlob == null)
                return null;

            MemoryStream memoryStream = new MemoryStream();
            await blockBlob.DownloadToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            memoryStream.Position = 0;
            return memoryStream;
        }

        /// <inheritdoc />
        public async Task<bool> TryDeleteImage(string imageName, ImageSizeType imageSizeType, CancellationToken cancellationToken)
        {
            ArgumentHelper.AssertNotNullOrEmpty(nameof(imageName), imageName);

            var blockBlob = GetBlobClient(imageName, imageSizeType);
            return await blockBlob.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Uri GetImageUri(string imageName, ImageSizeType imageSizeType)
        {
            ArgumentHelper.AssertNotNullOrEmpty(nameof(imageName), imageName);

            var blockBlob = GetBlobClient(imageName, imageSizeType);
            return blockBlob.Uri;
        }

        /// <inheritdoc />
        public async Task<bool> ImageExists(string imageName, ImageSizeType imageSizeType, CancellationToken cancellationToken)
        {
            ArgumentHelper.AssertNotNullOrEmpty(nameof(imageName), imageName);

            var blockBlob = GetBlobClient(imageName, imageSizeType);
            return await blockBlob.ExistsAsync(cancellationToken).ConfigureAwait(false);
        }

        private BlobClient GetBlobClient(string name, ImageSizeType imageSizeType)
        {
            if (imageSizeType == ImageSizeType.Original)
                return _imagesContainer.Value.GetBlobClient(name);

            return _thumbnailsContainer.Value.GetBlobClient(name);
        }
    }
}