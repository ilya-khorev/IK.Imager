using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Storage;
using Microsoft.Azure.Cosmos;

namespace IK.Imager.ImageMetadataStorage.CosmosDB
{
    public class ImageMetadataStorage: IImageMetadataStorage
    {
        private readonly ImageMetadataStorageConfiguration _configuration;
        private readonly Lazy<CosmosClient> _clientLazy;
        private CosmosClient Client => _clientLazy.Value;

        public ImageMetadataStorage(ImageMetadataStorageConfiguration configuration)
        {
            _configuration = configuration;
        }

        private Container _container;
        
        private async Task Initialize()
        {
            CosmosClient client = new CosmosClient(_configuration.Endpoint, _configuration.AuthKey);
            var databaseResponse = await client.CreateDatabaseIfNotExistsAsync(_configuration.DatabaseId);
                
            ContainerProperties containerProperties = new ContainerProperties(_configuration.ContainerId, _configuration.PartitionKeyPath);
            
            _container = (await databaseResponse.Database.CreateContainerIfNotExistsAsync(containerProperties,
                throughput: _configuration.ContainerThroughPutOnCreation)).Container;
        }

        public async Task SetMetadata(ImageMetadata metadata)
        {
            //todo consistency level
            await _container.CreateItemAsync(metadata, new PartitionKey(metadata.PartitionKey));
        }

        public Task<ImageMetadata> GetMetadata(ICollection<string> imageIds)
        {
            throw new NotImplementedException();
        }

        public Task SetMetadataForRemoval(string imageId)
        {
            throw new NotImplementedException();
        }

        public Task RemoveMetadata(string imageId)
        {
            throw new NotImplementedException();
        }
    }
}