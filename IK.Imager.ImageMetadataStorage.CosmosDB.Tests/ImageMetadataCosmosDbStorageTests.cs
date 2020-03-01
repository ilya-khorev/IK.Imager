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
        private readonly Random _random = new Random();
        
        public ImageMetadataCosmosDbStorageTests()
        {
            ImageMetadataCosmosDbStorageConfiguration configuration = new ImageMetadataCosmosDbStorageConfiguration
            {
                ConnectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
                ContainerId = "ImageMetadataContainer",
                ContainerThroughPutOnCreation = 1000,
                DatabaseId = "ImageMetadataDb"
            };
            
            _imageMetadataCosmosDbStorage = new ImageMetadataCosmosDbStorage(configuration);
        }
        
        [Fact]  
        public async Task AddImageMetadataTest()
        {
            ImageMetadata imageMetadata = GenerateItem();
            await _imageMetadataCosmosDbStorage.SetMetadata(imageMetadata, CancellationToken.None);
        }
        
        [Fact]  
        public async Task GetImageMetadataItemNoPartitionTest()
        {
            ImageMetadata imageMetadata = GenerateItem();
            await _imageMetadataCosmosDbStorage.SetMetadata(imageMetadata, CancellationToken.None);

            var receivedItem = await _imageMetadataCosmosDbStorage.GetMetadata(new List<string> {imageMetadata.Id}, CancellationToken.None);
            Assert.Single(receivedItem);
            Assert.Equal(imageMetadata, receivedItem[0]);
        }

        private ImageMetadata GenerateItem(string partitionKey = "partition1")
        {
            var item = new ImageMetadata
            {
                Id = Guid.NewGuid().ToString(),
                PartitionKey = partitionKey,
                Format = "jpg",
                Height = _random.Next(100, 1000),
                Width = _random.Next(100, 1000),
                Size = _random.Next(1000000, 9000000),
                Tags = new Dictionary<string, string>
                {
                    {"tag1", Guid.NewGuid().ToString()}, 
                    {"tag2", Guid.NewGuid().ToString()}
                },
                MD5Hash = Guid.NewGuid().ToString(),
                DateAddedUtc = DateTime.UtcNow,
                Name = Guid.NewGuid().ToString()
            };

            List<ImageThumbnail> thumbnails = new List<ImageThumbnail>();
            for (int i = 0; i < _random.Next(1, 5); i++)
            {
                thumbnails.Add(new ImageThumbnail
                {
                    Id = Guid.NewGuid().ToString(),
                    DateAddedUtc = DateTime.UtcNow,
                    Height = _random.Next(100, 1000),
                    Width = _random.Next(100, 1000),
                    MD5Hash = Guid.NewGuid().ToString(),
                    Size = _random.Next(1000000, 9000000)
                });
            }

            item.Thumbnails = thumbnails.ToArray();
            
            return item;
        }
    }
}