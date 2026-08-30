using Xunit;

namespace IK.Imager.Core.Tests;

/// <summary>
/// The blob path is the delivery url, so every one of these shapes is a public contract.
/// </summary>
public class ImageBlobPathTests
{
    private const string Tenant = "acme";
    private const string Collection = "photos";
    private const string Prefix = "8f2cd91a";
    private const string ImageId = "sku-1234";
    private const string Extension = "jpg";

    [Fact]
    public void Build_NoCollectionNoPrefix_IsTenantThenId()
    {
        Assert.Equal("acme/sku-1234.jpg",
            ImageBlobPath.Build(Tenant, null, null, ImageId, Extension));
    }

    [Fact]
    public void Build_CollectionOnly_PutsItAfterTheTenant()
    {
        Assert.Equal("acme/photos/sku-1234.jpg",
            ImageBlobPath.Build(Tenant, Collection, null, ImageId, Extension));
    }

    [Fact]
    public void Build_PrefixOnly_PutsItBeforeTheId()
    {
        Assert.Equal("acme/8f2cd91a/sku-1234.jpg",
            ImageBlobPath.Build(Tenant, null, Prefix, ImageId, Extension));
    }

    /// <summary>
    /// The collection comes first so that it stays a usable blob prefix - a random segment in front of it
    /// would make every collection-scoped storage operation impossible.
    /// </summary>
    [Fact]
    public void Build_CollectionAndPrefix_OrdersCollectionFirst()
    {
        Assert.Equal("acme/photos/8f2cd91a/sku-1234.jpg",
            ImageBlobPath.Build(Tenant, Collection, Prefix, ImageId, Extension));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Build_NoExtension_LeavesTheIdBare(string? extension)
    {
        Assert.Equal("acme/sku-1234", ImageBlobPath.Build(Tenant, null, null, ImageId, extension!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Build_EmptyCollectionOrPrefix_ContributesNoSegment(string? empty)
    {
        Assert.Equal("acme/sku-1234.jpg", ImageBlobPath.Build(Tenant, empty, empty, ImageId, Extension));
    }

    [Fact]
    public void BuildThumbnail_InheritsEverySegmentOfTheOriginal()
    {
        var original = ImageBlobPath.Build(Tenant, Collection, Prefix, ImageId, Extension);

        Assert.Equal("acme/photos/8f2cd91a/sku-1234_200.jpg",
            ImageBlobPath.BuildThumbnail(original, 200, Extension));
    }

    [Fact]
    public void BuildThumbnail_FormatChanged_UsesTheNewExtension()
    {
        Assert.Equal("acme/sku-1234_400.png",
            ImageBlobPath.BuildThumbnail("acme/sku-1234.bmp", 400, "png"));
    }

    /// <summary>
    /// Only a dot in the last segment is an extension - a dot in the tenant or the collection is not.
    /// </summary>
    [Fact]
    public void BuildThumbnail_DotInAnEarlierSegment_IsNotTreatedAsAnExtension()
    {
        Assert.Equal("my.tenant/my.collection/sku_200.jpg",
            ImageBlobPath.BuildThumbnail("my.tenant/my.collection/sku", 200, Extension));
    }

    [Fact]
    public void BuildThumbnail_SameWidthTwice_IsTheSamePath()
    {
        //deterministic, which is what makes regeneration overwrite instead of orphaning the previous set
        Assert.Equal(
            ImageBlobPath.BuildThumbnail("acme/sku-1234.jpg", 200, Extension),
            ImageBlobPath.BuildThumbnail("acme/sku-1234.jpg", 200, Extension));
    }
}
