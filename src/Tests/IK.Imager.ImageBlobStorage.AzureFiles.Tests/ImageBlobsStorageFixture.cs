using System;
using IK.Imager.ImageBlobStorage.AzureFiles;
using IK.Imager.TestsBase;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Options;

namespace IK.Imager.ImageStorage.AzureFiles.Tests
{
    public class ImageBlobsStorageFixture : IDisposable
    {
        public ImageBlobAzureRepository BlobImageRepository { get; }
        
        private readonly CloudBlobClient _cloudBlobClient;

        public ImageBlobsStorageFixture()
        {
            ImageAzureStorageSettings settings =
                new ImageAzureStorageSettings
                {
                    ConnectionString = Constants.AzureBlobStorage.ConnectionString,
                    ImagesContainerName = Constants.AzureBlobStorage.ImagesContainerName,
                    ThumbnailsContainerName = Constants.AzureBlobStorage.ThumbnailsContainerName
                };

            var blobClient = new AzureBlobClient(settings.ConnectionString);
            BlobImageRepository = new ImageBlobAzureRepository(new OptionsWrapper<ImageAzureStorageSettings>(settings), blobClient);
            
            var storageAccount = CloudStorageAccount.Parse(settings.ConnectionString);
            _cloudBlobClient = storageAccount.CreateCloudBlobClient();
        }

        public void Dispose()
        {
            foreach (var container in _cloudBlobClient.ListContainers())
            {
                container.DeleteIfExists();
            }
        }
    }
}