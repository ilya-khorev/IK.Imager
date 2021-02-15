using System;
using IK.Imager.TestsBase;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace IK.Imager.ImageMetadataStorage.CosmosDB.Tests
{
    public class ImageMetadataStorageFixture : IDisposable
    {
        public ImageMetadataCosmosDbRepository MetadataImageRepository { get; }
        
        private readonly CosmosClient _client;

        private readonly ImageMetadataCosmosDbStorageSettings _settings;
        
        public ImageMetadataStorageFixture()
        {
            _settings = new ImageMetadataCosmosDbStorageSettings
            {
                ConnectionString = Constants.CosmosDb.ConnectionString,
                ContainerId = Constants.CosmosDb.ContainerId,
                ContainerThroughPutOnCreation = Constants.CosmosDb.ContainerThroughPutOnCreation,
                DatabaseId = Constants.CosmosDb.DatabaseId
            };
            
            MetadataImageRepository = new ImageMetadataCosmosDbRepository(new CosmosDbClient(new OptionsWrapper<ImageMetadataCosmosDbStorageSettings>(_settings)));
            
            _client = new CosmosClient(_settings.ConnectionString);
        }
        
        public void Dispose()
        {
            var container = _client.GetContainer(_settings.DatabaseId, _settings.ContainerId);
            if (container == null)
                return;
            
            container.DeleteContainerAsync().GetAwaiter().GetResult();
            
            _client?.Dispose();
        }
    }
}