using System.Net;
using System.Threading.Tasks;
using IK.Imager.Api.Tests.Infrastructure;
using Xunit;

namespace IK.Imager.Api.Tests.Features.Upload;

/// <summary>
/// What a caller gets to decide about an image's identity and its url, end to end.
/// The url is a public contract, so every shape here is asserted against the real host.
/// </summary>
public class ImageIdentityTests(ImagerApiFixture fixture) : ImagerApiTests(fixture)
{
    private const string SuppliedId = "sku-1234";

    [Fact]
    public async Task Upload_SuppliedImageId_UsesItAsTheIdAndTheUrl()
    {
        var tenantId = NewTenantId();

        var uploaded = await Api.Upload(TestImages.Jpeg800X600, tenantId, imageId: SuppliedId);

        Assert.Equal(SuppliedId, uploaded.Id);
        Assert.EndsWith($"/{tenantId}/{SuppliedId}.jpg", uploaded.Url);
    }

    [Fact]
    public async Task Upload_NoImageId_GeneratesAnUnguessableOne()
    {
        var tenantId = NewTenantId();

        var uploaded = await Api.Upload(TestImages.Jpeg800X600, tenantId);

        Assert.Matches("^[a-f0-9]{32}$", uploaded.Id);
        Assert.EndsWith($"/{tenantId}/{uploaded.Id}.jpg", uploaded.Url);
    }

    [Fact]
    public async Task Upload_SameIdTwiceInOneTenant_ReturnsConflict()
    {
        var tenantId = NewTenantId();
        await Api.Upload(TestImages.Jpeg800X600, tenantId, imageId: SuppliedId);

        var response = await Api.PostUpload(TestImages.Jpeg800X600, tenantId, imageId: SuppliedId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>
    /// The unique prefix obscures the url; it does not widen identity. A caller might reasonably assume it
    /// lets them reuse an id, and it does not.
    /// </summary>
    [Fact]
    public async Task Upload_SameIdTwiceWithAUniquePrefix_StillReturnsConflict()
    {
        var tenantId = NewTenantId();
        await Api.Upload(TestImages.Jpeg800X600, tenantId, imageId: SuppliedId, addUniquePrefix: true);

        var response = await Api.PostUpload(TestImages.Jpeg800X600, tenantId, imageId: SuppliedId,
            addUniquePrefix: true);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>
    /// A collection organises images; it does not scope their identity.
    /// </summary>
    [Fact]
    public async Task Upload_SameIdInTwoCollectionsOfOneTenant_ReturnsConflict()
    {
        var tenantId = NewTenantId();
        await Api.Upload(TestImages.Jpeg800X600, tenantId, NewCollection(), imageId: SuppliedId);

        var response = await Api.PostUpload(TestImages.Jpeg800X600, tenantId, NewCollection(), imageId: SuppliedId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Upload_SameIdInTwoTenants_BothSucceedWithDistinctUrls()
    {
        var first = NewTenantId();
        var second = NewTenantId();

        var one = await Api.Upload(TestImages.Jpeg800X600, first, imageId: SuppliedId);
        var other = await Api.Upload(TestImages.Jpeg800X600, second, imageId: SuppliedId);

        Assert.Equal(one.Id, other.Id);
        Assert.NotEqual(one.Url, other.Url);
        Assert.EndsWith($"/{first}/{SuppliedId}.jpg", one.Url);
        Assert.EndsWith($"/{second}/{SuppliedId}.jpg", other.Url);
    }

    /// <summary>
    /// Deleting removes the metadata at once and the blobs off the bus a moment later, so re-uploading the
    /// same id finds a blob nothing owns. That is a replacement, not a conflict.
    /// </summary>
    [Fact]
    public async Task Upload_AfterDeletingTheSameId_Succeeds()
    {
        var tenantId = NewTenantId();
        var first = await Api.Upload(TestImages.Jpeg800X600, tenantId, imageId: SuppliedId);

        await Api.Delete(SuppliedId, tenantId);

        var second = await Api.Upload(TestImages.Jpeg800X600, tenantId, imageId: SuppliedId);

        Assert.Equal(first.Url, second.Url);
        Assert.Equal(SuppliedId, (await Api.LookupSingle(SuppliedId, tenantId)).Id);
    }

    [Fact]
    public async Task Upload_CollectionInPath_PutsItInTheUrl()
    {
        var tenantId = NewTenantId();
        var collection = NewCollection();

        var uploaded = await Api.Upload(TestImages.Jpeg800X600, tenantId, collection, SuppliedId,
            includeCollectionInPath: true);

        Assert.EndsWith($"/{tenantId}/{collection}/{SuppliedId}.jpg", uploaded.Url);
        Assert.Equal(collection, uploaded.Collection);
    }

    /// <summary>
    /// The flag is what puts a collection in the url - giving one alone only labels the image.
    /// </summary>
    [Fact]
    public async Task Upload_CollectionWithoutTheFlag_KeepsItOutOfTheUrl()
    {
        var tenantId = NewTenantId();
        var collection = NewCollection();

        var uploaded = await Api.Upload(TestImages.Jpeg800X600, tenantId, collection, SuppliedId);

        Assert.EndsWith($"/{tenantId}/{SuppliedId}.jpg", uploaded.Url);
        Assert.Equal(collection, uploaded.Collection);
        Assert.Equal(collection, (await Api.LookupSingle(SuppliedId, tenantId)).Collection);
    }

    [Fact]
    public async Task Upload_UniquePrefix_InsertsARandomSegmentBeforeTheId()
    {
        var tenantId = NewTenantId();

        var uploaded = await Api.Upload(TestImages.Jpeg800X600, tenantId, imageId: SuppliedId,
            addUniquePrefix: true);

        Assert.EndsWith($"/{SuppliedId}.jpg", uploaded.Url);
        Assert.Matches($"/{tenantId}/[a-f0-9]{{32}}/{SuppliedId}\\.jpg$", uploaded.Url);
    }

    [Fact]
    public async Task Upload_CollectionInPathAndUniquePrefix_OrdersCollectionFirst()
    {
        var tenantId = NewTenantId();
        var collection = NewCollection();

        var uploaded = await Api.Upload(TestImages.Jpeg800X600, tenantId, collection, SuppliedId,
            includeCollectionInPath: true, addUniquePrefix: true);

        Assert.Matches($"/{tenantId}/{collection}/[a-f0-9]{{32}}/{SuppliedId}\\.jpg$", uploaded.Url);
    }

    [Theory]
    [InlineData("SKU-1234")]
    [InlineData("with space")]
    [InlineData("with/slash")]
    [InlineData("-leading-hyphen")]
    [InlineData("trailing-dot.")]
    [InlineData("dot..dot")]
    public async Task Upload_ImageIdThatIsNotWellFormed_ReturnsValidationProblem(string imageId)
    {
        var problem = await ReadValidationProblem(
            await Api.PostUpload(TestImages.Jpeg800X600, NewTenantId(), imageId: imageId));

        Assert.Contains("ImageId", problem.Errors.Keys);
    }

    [Fact]
    public async Task Upload_IncludeCollectionInPathWithoutACollection_ReturnsValidationProblem()
    {
        var problem = await ReadValidationProblem(
            await Api.PostUpload(TestImages.Jpeg800X600, NewTenantId(), includeCollectionInPath: true));

        Assert.Contains("IncludeCollectionInPath", problem.Errors.Keys);
    }
}
