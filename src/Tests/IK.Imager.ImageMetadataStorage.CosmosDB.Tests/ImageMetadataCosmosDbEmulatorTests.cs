using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;
using Xunit;

namespace IK.Imager.ImageMetadataStorage.CosmosDB.Tests;

// These tests require a running Docker daemon - the Linux Cosmos DB emulator is started
// automatically by Testcontainers, see ImageMetadataStorageFixture. Nothing has to be installed
// or launched by hand.
//
// Naming convention:
// - The name of the method being tested
// - The scenario under which it's being tested (optional)
// - The expected behavior when the scenario is invoked
[Trait("Category", "Integration")]
[Collection(CosmosDbCollection.Name)]
public class ImageMetadataCosmosDbEmulatorTests
{
    //Seeded so that a failing test can be reproduced. Ids stay random to keep the tests isolated from each other.
    private readonly Random _random = new(42);
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
    public async Task SetMetadata_ExistingId_UpdatesExistingDocument()
    {
        ImageMetadata imageMetadata = GenerateItem();
        await _imageMetadataCosmosDbRepository.SetMetadata(imageMetadata, CancellationToken.None);

        imageMetadata.Name = "an updated name";
        await _imageMetadataCosmosDbRepository.SetMetadata(imageMetadata, CancellationToken.None);

        var receivedItems = await _imageMetadataCosmosDbRepository.GetMetadata(new[] { imageMetadata.Id },
            imageMetadata.ImageGroup, CancellationToken.None);
        var receivedItem = Assert.Single(receivedItems);
        Assert.Equal("an updated name", receivedItem.Name);
    }

    [Fact]
    public async Task GetMetadata_SearchWithoutImageGroup_ReturnCorrectResults()
    {
        List<ImageMetadata> imagesMetadata = new List<ImageMetadata>();
        List<string> ids = new List<string>();
        for (int i = 0; i < 8; i++)
        {
            ImageMetadata imageMetadata = GenerateItem("group_" + i);
            await _imageMetadataCosmosDbRepository.SetMetadata(imageMetadata, CancellationToken.None);
            ids.Add(imageMetadata.Id);
            imagesMetadata.Add(imageMetadata);
        }

        var receivedItems = await _imageMetadataCosmosDbRepository.GetMetadata(ids, CancellationToken.None);

        AssertSameItems(imagesMetadata, receivedItems);
    }

    [Fact]
    public async Task GetMetadata_SearchWithImageGroup_ReturnsOnlyRequestedGroup()
    {
        List<ImageMetadata> imagesMetadata = new List<ImageMetadata>();
        List<string> ids = new List<string>();
        List<string> partitions = new List<string>(3) { "group_1", "group_2", "group_3" };
        for (int i = 0; i < 12; i++)
        {
            ImageMetadata imageMetadata = GenerateItem(partitions[i % partitions.Count]);
            await _imageMetadataCosmosDbRepository.SetMetadata(imageMetadata, CancellationToken.None);
            ids.Add(imageMetadata.Id);
            imagesMetadata.Add(imageMetadata);
        }

        foreach (var partition in partitions)
        {
            var expectedItems = imagesMetadata.Where(x => x.ImageGroup == partition).ToList();
            var receivedItems = await _imageMetadataCosmosDbRepository.GetMetadata(ids, partition, CancellationToken.None);

            Assert.NotEmpty(expectedItems);
            AssertSameItems(expectedItems, receivedItems);
        }
    }

    [Fact]
    public async Task GetMetadata_WrongImageGroup_ReturnsEmpty()
    {
        ImageMetadata imageMetadata = GenerateItem("group_a");
        await _imageMetadataCosmosDbRepository.SetMetadata(imageMetadata, CancellationToken.None);

        var receivedItems = await _imageMetadataCosmosDbRepository.GetMetadata(new[] { imageMetadata.Id },
            "group_b", CancellationToken.None);

        Assert.Empty(receivedItems);
    }

    [Fact]
    public async Task GetMetadata_NotExistingIds_ReturnsEmpty()
    {
        var notExistingIds = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };

        var receivedItems = await _imageMetadataCosmosDbRepository.GetMetadata(notExistingIds, CancellationToken.None);

        Assert.Empty(receivedItems);
    }

    [Fact]
    public async Task GetMetadata_NullIdCollection_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _imageMetadataCosmosDbRepository.GetMetadata(null, CancellationToken.None));
    }

    [Fact]
    public async Task GetMetadata_EmptyIdCollection_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _imageMetadataCosmosDbRepository.GetMetadata(Array.Empty<string>(), CancellationToken.None));
    }

    [Fact]
    public async Task SetMetadata_NullMetadata_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _imageMetadataCosmosDbRepository.SetMetadata(null, CancellationToken.None));
    }

    [Theory]
    [InlineData(nameof(ImageMetadata.Id))]
    [InlineData(nameof(ImageMetadata.ImageGroup))]
    [InlineData(nameof(ImageMetadata.MimeType))]
    [InlineData(nameof(ImageMetadata.MD5Hash))]
    public async Task SetMetadata_EmptyRequiredProperty_ThrowsArgumentException(string propertyName)
    {
        ImageMetadata imageMetadata = GenerateItem();
        typeof(ImageMetadata).GetProperty(propertyName)!.SetValue(imageMetadata, string.Empty);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _imageMetadataCosmosDbRepository.SetMetadata(imageMetadata, CancellationToken.None));
    }

    [Theory]
    [InlineData(nameof(ImageMetadata.SizeBytes), 0)]
    [InlineData(nameof(ImageMetadata.SizeBytes), -1)]
    [InlineData(nameof(ImageMetadata.Width), 0)]
    [InlineData(nameof(ImageMetadata.Width), -1)]
    [InlineData(nameof(ImageMetadata.Height), 0)]
    [InlineData(nameof(ImageMetadata.Height), -1)]
    public async Task SetMetadata_NonPositiveDimension_ThrowsArgumentOutOfRangeException(string propertyName, int value)
    {
        ImageMetadata imageMetadata = GenerateItem();
        var property = typeof(ImageMetadata).GetProperty(propertyName)!;
        property.SetValue(imageMetadata, Convert.ChangeType(value, property.PropertyType));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _imageMetadataCosmosDbRepository.SetMetadata(imageMetadata, CancellationToken.None));
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

    [Fact]
    public async Task RemoveMetadata_NotExistingId_ReturnsFalse()
    {
        var removed = await _imageMetadataCosmosDbRepository.RemoveMetadata(Guid.NewGuid().ToString(), "group_1", CancellationToken.None);

        Assert.False(removed);
    }

    /// <summary>
    /// The image group is the partition key, so deleting without it cannot work.
    /// </summary>
    [Fact]
    public async Task RemoveMetadata_EmptyImageGroup_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _imageMetadataCosmosDbRepository.RemoveMetadata(Guid.NewGuid().ToString(), string.Empty, CancellationToken.None));
    }

    /// <summary>
    /// Cosmos DB gives no ordering guarantee unless the query has an explicit ORDER BY, so both sides
    /// are ordered before being compared. Assert.Equal over IEnumerable uses ImageMetadata.Equals and
    /// prints a readable diff when it fails.
    /// </summary>
    private static void AssertSameItems(IEnumerable<ImageMetadata> expected, IEnumerable<ImageMetadata> actual)
    {
        Assert.Equal(
            expected.OrderBy(x => x.Id, StringComparer.Ordinal),
            actual.OrderBy(x => x.Id, StringComparer.Ordinal));
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
