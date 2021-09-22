using System;
using Azure.Storage.Blobs;
using IK.Imager.ImageBlobStorage.AzureFiles;
using IK.Imager.TestsBase;
using Microsoft.Extensions.Options;

namespace IK.Imager.ImageStorage.AzureFiles.Tests
{
    public class ImageBlobsStorageFixture : IDisposable
    {
        public ImageBlobAzureRepository BlobImageRepository { get; }

        private readonly ImageAzureStorageSettings _settings;
        private readonly BlobServiceClient _cloudBlobClient;

        public ImageBlobsStorageFixture()
        { 
            _settings = new ImageAzureStorageSettings
                {
                    ConnectionString = Constants.AzureBlobStorage.ConnectionString,
                    ImagesContainerName = Constants.AzureBlobStorage.ImagesContainerName,
                    ThumbnailsContainerName = Constants.AzureBlobStorage.ThumbnailsContainerName
                };

            var blobClient = new AzureBlobClient(_settings.ConnectionString);
            BlobImageRepository =
                new ImageBlobAzureRepository(new OptionsWrapper<ImageAzureStorageSettings>(_settings), blobClient);

            _cloudBlobClient = new BlobServiceClient(_settings.ConnectionString);
        }

        public void Dispose()
        {
            _cloudBlobClient.DeleteBlobContainer(_settings.ImagesContainerName);
            _cloudBlobClient.DeleteBlobContainer(_settings.ThumbnailsContainerName);
        }
    }
}