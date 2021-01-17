using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.TestsBase;
using Microsoft.Extensions.Options;
using Xunit;

namespace IK.Imager.ImageMetadataStorage.CosmosDB.Tests
{
    public class ImageMetadataCosmosDbStorageTests
    {
        private readonly ImageMetadataCosmosDbStorage _imageMetadataCosmosDbStorage;
        private readonly Random _random = new Random();
        
        public ImageMetadataCosmosDbStorageTests()
        {
            ImageMetadataCosmosDbStorageSettings settings = new ImageMetadataCosmosDbStorageSettings
            {
                ConnectionString = Constants.CosmosDb.ConnectionString,
                ContainerId = Constants.CosmosDb.ContainerId,
                ContainerThroughPutOnCreation = Constants.CosmosDb.ContainerThroughPutOnCreation,
                DatabaseId = Constants.CosmosDb.DatabaseId
            };
   
            _imageMetadataCosmosDbStorage = new ImageMetadataCosmosDbStorage(new OptionsWrapper<ImageMetadataCosmosDbStorageSettings>(settings));
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
                ImageMetadata imageMetadata = GenerateItem("group_" + i);
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
            List<string> partitions = new List<string>(3) { "group_1", "group_2", "group_3" };
            for (int i = 0; i < _random.Next(10, 15); i++)
            {
                ImageMetadata imageMetadata = GenerateItem(partitions[_random.Next(0, 2)]);
                await _imageMetadataCosmosDbStorage.SetMetadata(imageMetadata, CancellationToken.None);
                ids.Add(imageMetadata.Id);
                imagesMetadata.Add(imageMetadata);
            }
 
            var firstPartitionItems = imagesMetadata.Where(x => x.ImageGroup == partitions[0]);
            var receivedItemsFirstPartition = await _imageMetadataCosmosDbStorage.GetMetadata(ids, partitions[0], CancellationToken.None);
            Assert.True(receivedItemsFirstPartition.SequenceEqual(firstPartitionItems));
        }

        [Fact]
        public async Task RemoveImageMetadataTest()
        {
            ImageMetadata imageMetadata = GenerateItem();
            await _imageMetadataCosmosDbStorage.SetMetadata(imageMetadata, CancellationToken.None);
  
            var removed = await _imageMetadataCosmosDbStorage.RemoveMetadata(imageMetadata.Id, imageMetadata.ImageGroup, CancellationToken.None);
            Assert.True(removed);
            
            removed = await _imageMetadataCosmosDbStorage.RemoveMetadata(imageMetadata.Id, imageMetadata.ImageGroup, CancellationToken.None);
            Assert.False(removed);
        } 

        private ImageMetadata GenerateItem(string imageGroup = "group_1")
        {
            var item = new ImageMetadata
            {
                Id = Guid.NewGuid().ToString(),
                ImageGroup = imageGroup,
                MimeType = "jpg",
                Height = _random.Next(100, 1000),
                Width = _random.Next(100, 1000),
                SizeBytes = _random.Next(1000000, 9000000),
                Tags = new Dictionary<string, string>
                {
                    {"tag1", Guid.NewGuid().ToString()}, {"tag2", Guid.NewGuid().ToString()}
                },
                MD5Hash = Guid.NewGuid().ToString(),
                DateAddedUtc = DateTime.UtcNow,
                Name = Guid.NewGuid().ToString(),
                Thumbnails = new List<ImageThumbnail>()
            };

            for (int i = 0; i < _random.Next(1, 5); i++)
            {
                item.Thumbnails.Add(new ImageThumbnail
                {
                    Id = Guid.NewGuid().ToString(),
                    DateAddedUtc = DateTime.UtcNow,
                    Height = _random.Next(100, 1000),
                    Width = _random.Next(100, 1000),
                    MD5Hash = Guid.NewGuid().ToString(),
                    SizeBytes = _random.Next(1000000, 9000000)
                });
            }
            
            return item;
        }
    }
}