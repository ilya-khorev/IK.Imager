using System;
using System.Collections.Generic;
using System.Linq;
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
        public async Task GetImagesMetadataNoPartitionTest()
        {
            List<ImageMetadata> imagesMetadata = new List<ImageMetadata>();
            List<string> ids = new List<string>();
            for (int i = 0; i < _random.Next(6, 10); i++)
            {
                ImageMetadata imageMetadata = GenerateItem("partition" + i);
                await _imageMetadataCosmosDbStorage.SetMetadata(imageMetadata, CancellationToken.None);
                ids.Add(imageMetadata.Id);
                imagesMetadata.Add(imageMetadata);
            }
            
            var receivedItems = await _imageMetadataCosmosDbStorage.GetMetadata(ids, CancellationToken.None);
            Assert.True(receivedItems.SequenceEqual(imagesMetadata));
        }
        
        [Fact]  
        public async Task GetImagesMetadataWithPartitionTest()
        {
            List<ImageMetadata> imagesMetadata = new List<ImageMetadata>();
            List<string> ids = new List<string>();
            List<string> partitions = new List<string>(3) { "partition1", "partition2", "partition3" };
            for (int i = 0; i < _random.Next(10, 15); i++)
            {
                ImageMetadata imageMetadata = GenerateItem(partitions[_random.Next(0, 2)]);
                await _imageMetadataCosmosDbStorage.SetMetadata(imageMetadata, CancellationToken.None);
                ids.Add(imageMetadata.Id);
                imagesMetadata.Add(imageMetadata);
            }
 
            var firstPartitionItems = imagesMetadata.Where(x => x.PartitionKey == partitions[0]);
            var receivedItemsFirstPartition = await _imageMetadataCosmosDbStorage.GetMetadata(ids, partitions[0], CancellationToken.None);
            Assert.True(receivedItemsFirstPartition.SequenceEqual(firstPartitionItems));
        }

        [Fact]
        public async Task RemoveImageMetadataTest()
        {
            ImageMetadata imageMetadata = GenerateItem();
            await _imageMetadataCosmosDbStorage.SetMetadata(imageMetadata, CancellationToken.None);

            bool deleted = await _imageMetadataCosmosDbStorage.RemoveMetadata(imageMetadata.Id, imageMetadata.PartitionKey, CancellationToken.None);
            Assert.True(deleted);
            
            deleted = await _imageMetadataCosmosDbStorage.RemoveMetadata(imageMetadata.Id, imageMetadata.PartitionKey, CancellationToken.None);
            Assert.False(deleted);
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