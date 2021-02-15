using Microsoft.Azure.Storage.Blob;

namespace IK.Imager.ImageBlobStorage.AzureFiles
{
    public interface IAzureBlobClient
    {
        CloudBlobContainer CreateContainerIfNotExists(string containerName);
    }
}