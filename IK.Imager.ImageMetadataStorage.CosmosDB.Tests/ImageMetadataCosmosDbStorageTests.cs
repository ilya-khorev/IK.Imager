using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;
using Xunit;

namespace IK.Imager.ImageMetadataStorage.CosmosDB.Tests
{
    public class ImageMetadataCosmosDbStorageTests
    {
        private readonly ImageMetadataCosmosDbStorage _imageMetadataCosmosDbStorage;
        public ImageMetadataCosmosDbStorageTests()
        {
            ImageMetadataCosmosDbStorageConfiguration configuration = new ImageMetadataCosmosDbStorageConfiguration
            {
                ConnectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
                ContainerId = "ImageMetadataContainer",
                ContainerThroughPutOnCreation = 4000,
                DatabaseId = "ImageMetadataDb",
                PartitionKeyPath = "/PartitionKey"
            };
            
            _imageMetadataCosmosDbStorage = new ImageMetadataCosmosDbStorage(configuration);
        }
        
        [Fact]  
        public async Task AddImageMetadataTest()
        {
            ImageMetadata imageMetadata = new ImageMetadata
            {
                Id = Guid.NewGuid().ToString(),
                PartitionKey = "partition1",
                Format = "jpg",
                Height = 500,
                Width = 500,
                Size = 1000000,
                Tags = new Dictionary<string, string> {{"tag1", "value1"}},
                MD5Hash = Guid.NewGuid().ToString(),
                DateAddedUtc = DateTime.UtcNow
            };

            await _imageMetadataCosmosDbStorage.SetMetadata(imageMetadata, CancellationToken.None);
        }
    }
}