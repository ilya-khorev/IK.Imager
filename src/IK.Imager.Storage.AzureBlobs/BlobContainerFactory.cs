using System;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace IK.Imager.Storage.AzureBlobs
{
    public class BlobContainerFactory : IBlobContainerFactory
    {
        private readonly BlobServiceClient _cloudBlobClient;

        public BlobContainerFactory(string connectionString)
        {
            ArgumentException.ThrowIfNullOrEmpty(connectionString);
            _cloudBlobClient = new BlobServiceClient(connectionString);
        }

        public BlobContainerClient CreateContainerIfNotExists(string containerName)
        {
            var blobContainerClient = _cloudBlobClient.GetBlobContainerClient(containerName);
            //setting up public access for this container so that it will be available by url
            blobContainerClient.CreateIfNotExists(PublicAccessType.Blob);
            return blobContainerClient;
        }
    }
}
