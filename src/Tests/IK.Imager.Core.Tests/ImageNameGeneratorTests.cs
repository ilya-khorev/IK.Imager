using System.Text.RegularExpressions;
using IK.Imager.Core.Abstractions;
using Xunit;

namespace IK.Imager.Core.Tests;

/// <summary>
/// The blob path is the delivery url, so every one of these shapes is a public contract.
/// </summary>
public class ImageNameGeneratorTests
{
    private const string Tenant = "acme";
    private const string Collection = "photos";
    private const string Prefix = "8f2cd91a";
    private const string ImageId = "sku-1234";
    private const string Extension = "jpg";

    private readonly IImageNameGenerator _generator = new ImageNameGenerator();

    [Fact]
    public void BuildBlobPath_NoCollectionNoPrefix_IsTenantThenId()
    {
        Assert.Equal("acme/sku-1234.jpg",
            _generator.BuildBlobPath(Tenant, null, null, ImageId, Extension));
    }

    [Fact]
    public void BuildBlobPath_CollectionOnly_PutsItAfterTheTenant()
    {
        Assert.Equal("acme/photos/sku-1234.jpg",
            _generator.BuildBlobPath(Tenant, Collection, null, ImageId, Extension));
    }

    [Fact]
    public void BuildBlobPath_PrefixOnly_PutsItBeforeTheId()
    {
        Assert.Equal("acme/8f2cd91a/sku-1234.jpg",
            _generator.BuildBlobPath(Tenant, null, Prefix, ImageId, Extension));
    }

    /// <summary>
    /// The collection comes first so that it stays a usable blob prefix - a random segment in front of it
    /// would make every collection-scoped storage operation impossible.
    /// </summary>
    [Fact]
    public void BuildBlobPath_CollectionAndPrefix_OrdersCollectionFirst()
    {
        Assert.Equal("acme/photos/8f2cd91a/sku-1234.jpg",
            _generator.BuildBlobPath(Tenant, Collection, Prefix, ImageId, Extension));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void BuildBlobPath_NoExtension_LeavesTheIdBare(string? extension)
    {
        Assert.Equal("acme/sku-1234", _generator.BuildBlobPath(Tenant, null, null, ImageId, extension!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void BuildBlobPath_EmptyCollectionOrPrefix_ContributesNoSegment(string? empty)
    {
        Assert.Equal("acme/sku-1234.jpg", _generator.BuildBlobPath(Tenant, empty, empty, ImageId, Extension));
    }

    [Fact]
    public void BuildThumbnailBlobPath_InheritsEverySegmentOfTheOriginal()
    {
        var original = _generator.BuildBlobPath(Tenant, Collection, Prefix, ImageId, Extension);

        Assert.Equal("acme/photos/8f2cd91a/sku-1234_200.jpg",
            _generator.BuildThumbnailBlobPath(original, 200, Extension));
    }

    [Fact]
    public void BuildThumbnailBlobPath_FormatChanged_UsesTheNewExtension()
    {
        Assert.Equal("acme/sku-1234_400.png",
            _generator.BuildThumbnailBlobPath("acme/sku-1234.bmp", 400, "png"));
    }

    /// <summary>
    /// Only a dot in the last segment is an extension - a dot in the tenant or the collection is not.
    /// </summary>
    [Fact]
    public void BuildThumbnailBlobPath_DotInAnEarlierSegment_IsNotTreatedAsAnExtension()
    {
        Assert.Equal("my.tenant/my.collection/sku_200.jpg",
            _generator.BuildThumbnailBlobPath("my.tenant/my.collection/sku", 200, Extension));
    }

    [Fact]
    public void BuildThumbnailBlobPath_SameWidthTwice_IsTheSamePath()
    {
        //deterministic, which is what makes regeneration overwrite instead of orphaning the previous set
        Assert.Equal(
            _generator.BuildThumbnailBlobPath("acme/sku-1234.jpg", 200, Extension),
            _generator.BuildThumbnailBlobPath("acme/sku-1234.jpg", 200, Extension));
    }

    /// <summary>
    /// A generated id is the only thing keeping a publicly readable blob private, so it has to satisfy the
    /// same charset the API demands of a supplied one and be long enough not to be guessed.
    /// </summary>
    [Fact]
    public void NewImageId_IsUnguessableAndUrlSafe()
    {
        var id = _generator.NewImageId();

        Assert.Matches(new Regex("^[a-z0-9]{38}$"), id);
        Assert.NotEqual(id, _generator.NewImageId());
    }

    [Fact]
    public void NewUniquePrefix_Is128BitsOfLowercaseHex()
    {
        var prefix = _generator.NewUniquePrefix();

        Assert.Matches(new Regex("^[a-f0-9]{32}$"), prefix);
        Assert.NotEqual(prefix, _generator.NewUniquePrefix());
    }
}
