using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;
using Xunit;

namespace IK.Imager.ImageMetadataStorage.CosmosDB.Tests
{
    // These tests require Azure Cosmos DB Emulator to be installed and launched 
    // https://aka.ms/cosmosdb-emulator
    // 
    // Naming convention:
    // - The name of the method being tested
    // - The scenario under which it's being tested (optional)
    // - The expected behavior when the scenario is invoked
    public class ImageMetadataCosmosDbEmulatorTests: IClassFixture<ImageMetadataStorageFixture>
    {
        private readonly Random _random = new Random();
        private readonly ImageMetadataCosmosDbRepository _imageMetadataCosmosDbRepository;
        public ImageMetadataCosmosDbEmulatorTests(ImageMetadataStorageFixture fixture)
        {
            _imageMetadataCosmosDbRepository = fixture.MetadataImageRepository;
        }
        
        [Fact]  
        public async Task SetMetadata_NoException()
        {
            ImageMetadata imageMetadata = GenerateItem();
            await _imageMetadataCosmosDbRepository.SetMetadata(imageMetadata, CancellationToken.None);
        }
        
        [Fact]  
        public async Task GetMetadata_SearchWithoutImageGroup_ReturnCorrectResults()
        {
            List<ImageMetadata> imagesMetadata = new List<ImageMetadata>();
            List<string> ids = new List<string>();
            for (int i = 0; i < _random.Next(6, 10); i++)
            {
                ImageMetadata imageMetadata = GenerateItem("group_" + i);
                await _imageMetadataCosmosDbRepository.SetMetadata(imageMetadata, CancellationToken.None);
                ids.Add(imageMetadata.Id);
                imagesMetadata.Add(imageMetadata);
            }
            
            var receivedItems = await _imageMetadataCosmosDbRepository.GetMetadata(ids, CancellationToken.None);
            Assert.True(receivedItems.SequenceEqual(imagesMetadata));
        }
        
        [Fact]  
        public async Task GetMetadata_SearchWithImageGroup_ReturnCorrectResults()
        {
            List<ImageMetadata> imagesMetadata = new List<ImageMetadata>();
            List<string> ids = new List<string>();
            List<string> partitions = new List<string>(3) { "group_1", "group_2", "group_3" };
            for (int i = 0; i < _random.Next(10, 15); i++)
            {
                ImageMetadata imageMetadata = GenerateItem(partitions[_random.Next(0, 2)]);
                await _imageMetadataCosmosDbRepository.SetMetadata(imageMetadata, CancellationToken.None);
                ids.Add(imageMetadata.Id);
                imagesMetadata.Add(imageMetadata);
            }
 
            var firstPartitionItems = imagesMetadata.Where(x => x.ImageGroup == partitions[0]);
            var receivedItemsFirstPartition = await _imageMetadataCosmosDbRepository.GetMetadata(ids, partitions[0], CancellationToken.None);
            Assert.True(receivedItemsFirstPartition.SequenceEqual(firstPartitionItems));
        }

        [Fact]
        public async Task RemoveMetadata_ExistingObject_ReturnsTrue()
        {
            ImageMetadata imageMetadata = GenerateItem();
            await _imageMetadataCosmosDbRepository.SetMetadata(imageMetadata, CancellationToken.None);
  
            var removed = await _imageMetadataCosmosDbRepository.RemoveMetadata(imageMetadata.Id, imageMetadata.ImageGroup, CancellationToken.None);
            Assert.True(removed);
        } 
        
        [Fact]
        public async Task RemoveMetadata_DeletedObject_ReturnsFalse()
        {
            ImageMetadata imageMetadata = GenerateItem();
            await _imageMetadataCosmosDbRepository.SetMetadata(imageMetadata, CancellationToken.None);
            await _imageMetadataCosmosDbRepository.RemoveMetadata(imageMetadata.Id, imageMetadata.ImageGroup, CancellationToken.None);

            var removed = await _imageMetadataCosmosDbRepository.RemoveMetadata(imageMetadata.Id, imageMetadata.ImageGroup, CancellationToken.None);
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