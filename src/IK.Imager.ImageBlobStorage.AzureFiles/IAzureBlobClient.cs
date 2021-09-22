using Azure.Storage.Blobs;

namespace IK.Imager.ImageBlobStorage.AzureFiles
{
    public interface IAzureBlobClient
    {
        BlobContainerClient CreateContainerIfNotExists(string containerName);
    }
}