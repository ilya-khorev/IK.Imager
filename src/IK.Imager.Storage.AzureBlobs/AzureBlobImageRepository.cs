using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Options;

namespace IK.Imager.Storage.AzureBlobs
{
    public class AzureBlobImageRepository : IImageBlobRepository
    {
        private readonly Lazy<BlobContainerClient> _imagesContainer;
        private readonly Lazy<BlobContainerClient> _thumbnailsContainer;

        public AzureBlobImageRepository(IOptions<AzureBlobStorageSettings> settings, IBlobContainerFactory blobClient)
        {
            ArgumentNullException.ThrowIfNull(settings);

            _imagesContainer = new Lazy<BlobContainerClient>(() => blobClient.CreateContainerIfNotExists(settings.Value.ImagesContainerName.ToLowerInvariant()));
            _thumbnailsContainer = new Lazy<BlobContainerClient>(() => blobClient.CreateContainerIfNotExists(settings.Value.ThumbnailsContainerName.ToLowerInvariant()));
        }

        /// <inheritdoc />
        public async Task<BlobUploadResult> UploadImage(string imageName, Stream imageStream, ImageVariant variant,
            string imageContentType, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(imageName);
            ArgumentNullException.ThrowIfNull(imageStream);

            var blobClient = GetBlobClient(imageName, variant);

            imageStream.Position = 0;
            //the content type has to go on with the upload - these blobs are served straight to browsers
            //by url, and without it every one of them comes back as application/octet-stream.
            //IfNoneMatch is what the stream-only overload sets for you: this overload leaves the conditions
            //null and would silently overwrite an existing blob instead of failing with a 409.
            var uploadResult = await blobClient.UploadAsync(imageStream,
                new BlobHttpHeaders { ContentType = imageContentType },
                conditions: new BlobRequestConditions { IfNoneMatch = ETag.All },
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return new BlobUploadResult
            {
                Hash = Convert.ToBase64String(uploadResult.Value.ContentHash),
                DateAdded = uploadResult.Value.LastModified,
                Url = blobClient.Uri
            };
        }

        /// <inheritdoc />
        public async Task<MemoryStream?> DownloadImage(string imageName, ImageVariant variant,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(imageName);

            var blockBlob = GetBlobClient(imageName, variant);

            MemoryStream memoryStream = new MemoryStream();
            try
            {
                await blockBlob.DownloadToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            }
            catch (RequestFailedException e) when (e.Status == (int)HttpStatusCode.NotFound)
            {
                //IImageBlobRepository documents a missing image as null. GetBlobClient always hands back a
                //client, so the only way to learn the blob is not there is to ask for it.
                await memoryStream.DisposeAsync().ConfigureAwait(false);
                return null;
            }

            memoryStream.Position = 0;
            return memoryStream;
        }

        /// <inheritdoc />
        public async Task<bool> TryDeleteImage(string imageName, ImageVariant variant, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(imageName);

            var blockBlob = GetBlobClient(imageName, variant);
            return await blockBlob.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Uri GetImageUri(string imageName, ImageVariant variant)
        {
            ArgumentException.ThrowIfNullOrEmpty(imageName);

            var blockBlob = GetBlobClient(imageName, variant);
            return blockBlob.Uri;
        }

        /// <inheritdoc />
        public async Task<bool> ImageExists(string imageName, ImageVariant variant, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(imageName);

            var blockBlob = GetBlobClient(imageName, variant);
            return await blockBlob.ExistsAsync(cancellationToken).ConfigureAwait(false);
        }

        private BlobClient GetBlobClient(string name, ImageVariant variant)
        {
            if (variant == ImageVariant.Original)
                return _imagesContainer.Value.GetBlobClient(name);

            return _thumbnailsContainer.Value.GetBlobClient(name);
        }
    }
}
