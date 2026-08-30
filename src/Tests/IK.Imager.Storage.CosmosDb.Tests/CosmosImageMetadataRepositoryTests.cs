using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace IK.Imager.Storage.CosmosDb.Tests;

// These tests require a running Docker daemon - the Linux Cosmos DB emulator is started
// automatically by Testcontainers, see CosmosDbFixture. Nothing has to be installed
// or launched by hand.
//
// Naming convention:
// - The name of the method being tested
// - The scenario under which it's being tested (optional)
// - The expected behavior when the scenario is invoked
[Trait("Category", "Integration")]
[Collection(CosmosDbCollection.Name)]
public class CosmosImageMetadataRepositoryTests
{
    //Seeded so that a failing test can be reproduced. Ids stay random to keep the tests isolated from each other.
    private readonly Random _random = new(42);
    private readonly CosmosImageMetadataRepository _imageMetadataCosmosDbRepository;
    private readonly CosmosDbFixture _fixture;

    public CosmosImageMetadataRepositoryTests(CosmosDbFixture fixture)
    {
        _fixture = fixture;
        _imageMetadataCosmosDbRepository = fixture.Repository;
    }

    [Fact]
    public async Task CreateMetadata_ValidMetadata_StoresDocument()
    {
        ImageMetadata imageMetadata = GenerateItem();
        await _imageMetadataCosmosDbRepository.CreateMetadata(imageMetadata, CancellationToken.None);

        var receivedItems = await _imageMetadataCosmosDbRepository.GetMetadata(new[] { imageMetadata.Id },
            imageMetadata.TenantId, CancellationToken.None);

        AssertSameItems([imageMetadata], receivedItems);
    }

    /// <summary>
    /// The partition key is (TenantId, id) and a logical partition holds exactly one document, so an id is
    /// unique within its tenant and the database is what enforces it.
    /// </summary>
    /// <summary>
    /// The stored document keeps the property names earlier versions wrote - members unchanged, the id as
    /// "id", the image type as a number. System.Text.Json replaced the SDK default serializer here, and the
    /// two only agree while ImageMetadataSerialization applies no naming policy. Adding one would orphan
    /// every document already stored, and would move "/TenantId" out from under the partition key, so this
    /// reads the raw document rather than going through the repository that wrote it.
    /// </summary>
    [Fact]
    public async Task CreateMetadata_ValidMetadata_StoresDocumentWithUnchangedPropertyNames()
    {
        ImageMetadata imageMetadata = GenerateItem();
        imageMetadata.ImageType = ImageType.JPEG;
        await _imageMetadataCosmosDbRepository.CreateMetadata(imageMetadata, CancellationToken.None);

        var container = _fixture.CosmosClient.GetContainer(_fixture.Settings.DatabaseId, _fixture.Settings.ContainerId);
        using var response = await container.ReadItemStreamAsync(imageMetadata.Id,
            new PartitionKeyBuilder().Add(imageMetadata.TenantId).Add(imageMetadata.Id).Build());

        Assert.True(response.IsSuccessStatusCode);

        using var document = await JsonDocument.ParseAsync(response.Content);
        var root = document.RootElement;

        Assert.Equal(imageMetadata.Id, root.GetProperty("id").GetString());
        Assert.Equal(imageMetadata.TenantId, root.GetProperty("TenantId").GetString());
        Assert.Equal(imageMetadata.Collection, root.GetProperty("Collection").GetString());
        Assert.Equal(imageMetadata.BlobPath, root.GetProperty("BlobPath").GetString());
        Assert.Equal(imageMetadata.SizeBytes, root.GetProperty("SizeBytes").GetInt64());
        Assert.Equal((int)ImageType.JPEG, root.GetProperty("ImageType").GetInt32());
        Assert.Equal(imageMetadata.Thumbnails![0].BlobPath,
            root.GetProperty("Thumbnails")[0].GetProperty("BlobPath").GetString());
    }

    [Fact]
    public async Task CreateMetadata_ExistingIdInSameTenant_ThrowsImageAlreadyExists()
    {
        ImageMetadata imageMetadata = GenerateItem();
        await _imageMetadataCosmosDbRepository.CreateMetadata(imageMetadata, CancellationToken.None);

        var duplicate = GenerateItem(imageMetadata.TenantId);
        duplicate.Id = imageMetadata.Id;

        var exception = await Assert.ThrowsAsync<ImageAlreadyExistsException>(() =>
            _imageMetadataCosmosDbRepository.CreateMetadata(duplicate, CancellationToken.None));

        Assert.Equal(imageMetadata.Id, exception.ImageId);
        Assert.Equal(imageMetadata.TenantId, exception.TenantId);
    }

    /// <summary>
    /// Uniqueness is scoped to the tenant, so two tenants can each hold the same id.
    /// </summary>
    [Fact]
    public async Task CreateMetadata_SameIdInAnotherTenant_StoresBothDocuments()
    {
        ImageMetadata first = GenerateItem("tenant-a-" + Guid.NewGuid().ToString("N"));
        await _imageMetadataCosmosDbRepository.CreateMetadata(first, CancellationToken.None);

        ImageMetadata second = GenerateItem("tenant-b-" + Guid.NewGuid().ToString("N"));
        second.Id = first.Id;
        await _imageMetadataCosmosDbRepository.CreateMetadata(second, CancellationToken.None);

        AssertSameItems([first],
            await _imageMetadataCosmosDbRepository.GetMetadata([first.Id], first.TenantId, CancellationToken.None));
        AssertSameItems([second],
            await _imageMetadataCosmosDbRepository.GetMetadata([second.Id], second.TenantId, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateMetadata_ExistingId_OverwritesExistingDocument()
    {
        ImageMetadata imageMetadata = GenerateItem();
        await _imageMetadataCosmosDbRepository.CreateMetadata(imageMetadata, CancellationToken.None);

        imageMetadata.BlobPath = "an/updated/path.jpg";
        await _imageMetadataCosmosDbRepository.UpdateMetadata(imageMetadata, CancellationToken.None);

        var receivedItems = await _imageMetadataCosmosDbRepository.GetMetadata(new[] { imageMetadata.Id },
            imageMetadata.TenantId, CancellationToken.None);
        var receivedItem = Assert.Single(receivedItems);
        Assert.Equal("an/updated/path.jpg", receivedItem.BlobPath);
    }

    [Fact]
    public async Task GetMetadata_ManyIds_ReturnsRequestedImages()
    {
        const string tenantId = "lookup-tenant";
        List<ImageMetadata> imagesMetadata = [];
        List<string> ids = [];
        for (int i = 0; i < 8; i++)
        {
            ImageMetadata imageMetadata = GenerateItem(tenantId);
            await _imageMetadataCosmosDbRepository.CreateMetadata(imageMetadata, CancellationToken.None);
            ids.Add(imageMetadata.Id);
            imagesMetadata.Add(imageMetadata);
        }

        var receivedItems = await _imageMetadataCosmosDbRepository.GetMetadata(ids, tenantId, CancellationToken.None);

        AssertSameItems(imagesMetadata, receivedItems);
    }

    /// <summary>
    /// Tenant isolation, asserted on the read path: an id belonging to another tenant resolves to nothing,
    /// because the tenant is the first level of the point read's partition key.
    /// </summary>
    [Fact]
    public async Task GetMetadata_AnotherTenant_ReturnsEmpty()
    {
        ImageMetadata imageMetadata = GenerateItem("tenant-a");
        await _imageMetadataCosmosDbRepository.CreateMetadata(imageMetadata, CancellationToken.None);

        var receivedItems = await _imageMetadataCosmosDbRepository.GetMetadata(new[] { imageMetadata.Id },
            "tenant-b", CancellationToken.None);

        Assert.Empty(receivedItems);
    }

    [Fact]
    public async Task GetMetadata_NotExistingIds_ReturnsEmpty()
    {
        var notExistingIds = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };

        var receivedItems = await _imageMetadataCosmosDbRepository.GetMetadata(notExistingIds, "tenant-a",
            CancellationToken.None);

        Assert.Empty(receivedItems);
    }

    [Fact]
    public async Task GetMetadata_NullIdCollection_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _imageMetadataCosmosDbRepository.GetMetadata(null!, "tenant-a", CancellationToken.None));
    }

    [Fact]
    public async Task GetMetadata_EmptyIdCollection_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _imageMetadataCosmosDbRepository.GetMetadata(Array.Empty<string>(), "tenant-a", CancellationToken.None));
    }

    /// <summary>
    /// The tenant is the first level of the partition key, so a read without it cannot work.
    /// </summary>
    [Fact]
    public async Task GetMetadata_EmptyTenant_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _imageMetadataCosmosDbRepository.GetMetadata([Guid.NewGuid().ToString()], string.Empty,
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateMetadata_NullMetadata_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _imageMetadataCosmosDbRepository.CreateMetadata(null!, CancellationToken.None));
    }

    [Theory]
    [InlineData(nameof(ImageMetadata.Id))]
    [InlineData(nameof(ImageMetadata.TenantId))]
    [InlineData(nameof(ImageMetadata.BlobPath))]
    [InlineData(nameof(ImageMetadata.MimeType))]
    [InlineData(nameof(ImageMetadata.MD5Hash))]
    public async Task CreateMetadata_EmptyRequiredProperty_ThrowsArgumentException(string propertyName)
    {
        ImageMetadata imageMetadata = GenerateItem();
        typeof(ImageMetadata).GetProperty(propertyName)!.SetValue(imageMetadata, string.Empty);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _imageMetadataCosmosDbRepository.CreateMetadata(imageMetadata, CancellationToken.None));
    }

    [Theory]
    [InlineData(nameof(ImageMetadata.SizeBytes), 0)]
    [InlineData(nameof(ImageMetadata.SizeBytes), -1)]
    [InlineData(nameof(ImageMetadata.Width), 0)]
    [InlineData(nameof(ImageMetadata.Width), -1)]
    [InlineData(nameof(ImageMetadata.Height), 0)]
    [InlineData(nameof(ImageMetadata.Height), -1)]
    public async Task CreateMetadata_NonPositiveDimension_ThrowsArgumentOutOfRangeException(string propertyName, int value)
    {
        ImageMetadata imageMetadata = GenerateItem();
        var property = typeof(ImageMetadata).GetProperty(propertyName)!;
        property.SetValue(imageMetadata, Convert.ChangeType(value, property.PropertyType));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _imageMetadataCosmosDbRepository.CreateMetadata(imageMetadata, CancellationToken.None));
    }

    [Fact]
    public async Task RemoveMetadata_ExistingObject_ReturnsTrue()
    {
        ImageMetadata imageMetadata = GenerateItem();
        await _imageMetadataCosmosDbRepository.CreateMetadata(imageMetadata, CancellationToken.None);

        var removed = await _imageMetadataCosmosDbRepository.RemoveMetadata(imageMetadata.Id, imageMetadata.TenantId,
            CancellationToken.None);
        Assert.True(removed);
    }

    [Fact]
    public async Task RemoveMetadata_DeletedObject_ReturnsFalse()
    {
        ImageMetadata imageMetadata = GenerateItem();
        await _imageMetadataCosmosDbRepository.CreateMetadata(imageMetadata, CancellationToken.None);
        await _imageMetadataCosmosDbRepository.RemoveMetadata(imageMetadata.Id, imageMetadata.TenantId,
            CancellationToken.None);

        var removed = await _imageMetadataCosmosDbRepository.RemoveMetadata(imageMetadata.Id, imageMetadata.TenantId,
            CancellationToken.None);
        Assert.False(removed);
    }

    [Fact]
    public async Task RemoveMetadata_AnotherTenant_ReturnsFalse()
    {
        ImageMetadata imageMetadata = GenerateItem("tenant-a");
        await _imageMetadataCosmosDbRepository.CreateMetadata(imageMetadata, CancellationToken.None);

        var removed = await _imageMetadataCosmosDbRepository.RemoveMetadata(imageMetadata.Id, "tenant-b",
            CancellationToken.None);

        Assert.False(removed);
    }

    [Fact]
    public async Task RemoveMetadata_NotExistingId_ReturnsFalse()
    {
        var removed = await _imageMetadataCosmosDbRepository.RemoveMetadata(Guid.NewGuid().ToString(), "tenant-a",
            CancellationToken.None);

        Assert.False(removed);
    }

    /// <summary>
    /// The tenant is the first level of the partition key, so deleting without it cannot work.
    /// </summary>
    [Fact]
    public async Task RemoveMetadata_EmptyTenant_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _imageMetadataCosmosDbRepository.RemoveMetadata(Guid.NewGuid().ToString(), string.Empty,
                CancellationToken.None));
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

    private ImageMetadata GenerateItem(string tenantId = "tenant-1")
    {
        var item = new ImageMetadata
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            Collection = "collection-1",
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
            BlobPath = Guid.NewGuid().ToString(),
            Thumbnails = new List<ImageThumbnail>()
        };

        for (int i = 0; i < _random.Next(1, 5); i++)
        {
            item.Thumbnails.Add(new ImageThumbnail
            {
                Id = Guid.NewGuid().ToString(),
                BlobPath = Guid.NewGuid().ToString(),
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
