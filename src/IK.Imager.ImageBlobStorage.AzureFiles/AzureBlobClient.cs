using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using IK.Imager.Utils;

namespace IK.Imager.ImageBlobStorage.AzureFiles
{
    public class AzureBlobClient : IAzureBlobClient
    {
        private readonly BlobServiceClient _cloudBlobClient;
        
        public AzureBlobClient(string connectionString)
        {
            ArgumentHelper.AssertNotNullOrEmpty(nameof(connectionString), connectionString);
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