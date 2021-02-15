using IK.Imager.Utils;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace IK.Imager.ImageBlobStorage.AzureFiles
{
    public class AzureBlobClient : IAzureBlobClient
    {
        private readonly CloudBlobClient _cloudBlobClient;
        
        public AzureBlobClient(string connectionString)
        {
            ArgumentHelper.AssertNotNullOrEmpty(nameof(connectionString), connectionString);
            
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            _cloudBlobClient = storageAccount.CreateCloudBlobClient();
        }
        
        public CloudBlobContainer CreateContainerIfNotExists(string containerName)
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
    }
}