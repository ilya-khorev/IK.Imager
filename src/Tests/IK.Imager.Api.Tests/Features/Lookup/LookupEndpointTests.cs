using System;
using System.Linq;
using System.Threading.Tasks;
using IK.Imager.Api.Tests.Infrastructure;
using Xunit;

namespace IK.Imager.Api.Tests.Features.Lookup;

/// <summary>
/// POST /images/lookup - fetching images by id. There is no querying or filtering, hence "lookup".
/// </summary>
[Trait("Category", "Integration")]
public class LookupEndpointTests(ImagerApiFixture fixture) : ImagerApiTests(fixture)
{
    [Fact]
    public async Task LookupByIds_TenantOfTheUploadedImage_ReturnsItsFullMetadata()
    {
        var tenantId = NewTenantId();
        var uploaded = await Api.Upload(TestImages.Jpeg1200X900, tenantId);

        var found = await Api.LookupSingle(uploaded.Id, tenantId);

        Assert.Equal(uploaded.Id, found.Id);
        Assert.Equal(uploaded.Url, found.Url);
        Assert.Equal(uploaded.Hash, found.Hash);
        Assert.Equal(uploaded.Bytes, found.Bytes);
        Assert.Equal(uploaded.Width, found.Width);
        Assert.Equal(uploaded.Height, found.Height);
        Assert.Equal(uploaded.MimeType, found.MimeType);
    }

    [Fact]
    public async Task LookupByIds_ImageUploadedIntoACollection_ReturnsIt()
    {
        var tenantId = NewTenantId();
        var uploaded = await Api.Upload(TestImages.Jpeg800X600, tenantId, NewCollection());

        var found = await Api.LookupSingle(uploaded.Id, tenantId);

        Assert.Equal(uploaded.Id, found.Id);
    }

    [Fact]
    public async Task LookupByIds_SeveralIds_ReturnsEveryOneOfThem()
    {
        var tenantId = NewTenantId();
        var first = await Api.Upload(TestImages.Jpeg1200X900, tenantId);
        var second = await Api.Upload(TestImages.Jpeg800X600, tenantId);
        var third = await Api.Upload(TestImages.Png1000X1000, tenantId);

        var result = await Api.Lookup([first.Id, second.Id, third.Id], tenantId);

        Assert.Equal(3, result.Images.Count);
        //sorted on both sides: the repository returns whatever order the Cosmos query yields
        Assert.Equal(
            new[] { first.Id, second.Id, third.Id }.OrderBy(id => id),
            result.Images.Select(image => image.Id).OrderBy(id => id));
    }

    [Fact]
    public async Task LookupByIds_IdThatWasNeverUploaded_ReturnsAnEmptyResult()
    {
        var result = await Api.Lookup([Guid.NewGuid().ToString("N")], NewTenantId());

        Assert.Empty(result.Images);
    }

    [Fact]
    public async Task LookupByIds_ImageGroupOfAnotherTest_DoesNotReturnTheImage()
    {
        var uploaded = await Api.Upload(TestImages.Jpeg800X600, NewTenantId());

        var result = await Api.Lookup([uploaded.Id], NewTenantId());

        Assert.Empty(result.Images);
    }

    [Fact]
    public async Task LookupByIds_MoreThanTwoHundredIds_ReturnsValidationProblem()
    {
        var tooManyIds = Enumerable.Range(0, 201).Select(_ => Guid.NewGuid().ToString("N")).ToArray();

        var problem = await ReadValidationProblem(
            await Api.PostLookup(new { ImageIds = tooManyIds }, NewTenantId()));

        Assert.Contains("ImageIds", problem.Errors.Keys);
    }

    [Fact]
    public async Task LookupByIds_ExactlyTwoHundredIds_IsAccepted()
    {
        var maximumIds = Enumerable.Range(0, 200).Select(_ => Guid.NewGuid().ToString("N")).ToArray();

        var result = await Api.Lookup(maximumIds, NewTenantId());

        Assert.Empty(result.Images);
    }

    [Fact]
    public async Task LookupByIds_EmptyIdArray_ReturnsValidationProblem()
    {
        var problem = await ReadValidationProblem(
            await Api.PostLookup(new { ImageIds = Array.Empty<string>() }, NewTenantId()));

        Assert.Contains("ImageIds", problem.Errors.Keys);
    }

    [Fact]
    public async Task LookupByIds_NoTenantHeader_ReturnsValidationProblem()
    {
        var problem = await ReadValidationProblem(
            await Api.PostLookup(new { ImageIds = new[] { Guid.NewGuid().ToString("N") } }, tenantId: null));

        Assert.Contains("TenantId", problem.Errors.Keys);
    }

    /// <summary>
    /// Ids are only ever resolved inside the tenant that asked, which is the whole point of the tenant being
    /// the first level of the partition key.
    /// </summary>
    [Fact]
    public async Task LookupByIds_ImageOfAnotherTenant_ReturnsNothing()
    {
        var image = await Api.Upload(TestImages.Jpeg800X600, NewTenantId());

        var result = await Api.Lookup([image.Id], NewTenantId());

        Assert.Empty(result.Images);
    }
}
