using Azure.Storage.Blobs;

namespace IK.Imager.Storage.AzureBlobs
{
    public interface IBlobContainerFactory
    {
        BlobContainerClient CreateContainerIfNotExists(string containerName);
    }
}
