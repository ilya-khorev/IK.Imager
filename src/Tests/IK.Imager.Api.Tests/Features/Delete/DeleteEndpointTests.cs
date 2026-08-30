using System;
using System.Net;
using System.Threading.Tasks;
using IK.Imager.Api.Tests.Infrastructure;
using Xunit;

namespace IK.Imager.Api.Tests.Features.Delete;

/// <summary>
/// DELETE /images/{imageId}.
///
/// The call removes the metadata only, which is what makes the image vanish from lookups straight away;
/// the blobs go afterwards, when RemoveImageFilesConsumer consumes the event.
/// </summary>
[Trait("Category", "Integration")]
public class DeleteEndpointTests(ImagerApiFixture fixture) : ImagerApiTests(fixture)
{
    [Fact]
    public async Task DeleteImage_UploadedImage_ReturnsNoContent()
    {
        var tenantId = NewTenantId();
        var uploaded = await Api.Upload(TestImages.Jpeg800X600, tenantId);

        var response = await Api.SendDelete(uploaded.Id, tenantId);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteImage_UploadedImage_RemovesItFromLookupImmediately()
    {
        var tenantId = NewTenantId();
        var uploaded = await Api.Upload(TestImages.Jpeg800X600, tenantId);

        await Api.Delete(uploaded.Id, tenantId);
        var result = await Api.Lookup([uploaded.Id], tenantId);

        Assert.Empty(result.Images);
    }

    [Fact]
    public async Task DeleteImage_OfAnotherTenant_ReturnsNotFound()
    {
        var tenantId = NewTenantId();
        var uploaded = await Api.Upload(TestImages.Jpeg800X600, tenantId);

        var response = await Api.SendDelete(uploaded.Id, NewTenantId());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        //and the image is untouched in the tenant that owns it
        Assert.Single((await Api.Lookup([uploaded.Id], tenantId)).Images);
    }

    [Fact]
    public async Task DeleteImage_ImageWithThumbnails_RemovesTheFilesWithoutFaulting()
    {
        var tenantId = NewTenantId();
        var uploaded = await Api.Upload(TestImages.Jpeg1200X900, tenantId);
        await Fixture.ConsumedEvents.ThumbnailsGenerated(uploaded.Id);

        await Api.Delete(uploaded.Id, tenantId);

        //the handler deletes the original and every thumbnail blob; a throw inside it surfaces here rather
        //than disappearing into the bus
        await Fixture.ConsumedEvents.FilesRemoved(uploaded.Id);
    }

    /// <summary>
    /// The blob removal publishes a second event that PurgeCdnFilesConsumer picks up. No CDN is configured
    /// here, so the purge itself is a no-op - what this covers is that the chain is wired at all.
    /// </summary>
    [Fact]
    public async Task DeleteImage_ImageWithThumbnails_PurgesTheCdnAfterTheFilesAreRemoved()
    {
        var tenantId = NewTenantId();
        var uploaded = await Api.Upload(TestImages.Jpeg1200X900, tenantId);
        await Fixture.ConsumedEvents.ThumbnailsGenerated(uploaded.Id);

        await Api.Delete(uploaded.Id, tenantId);

        await Fixture.ConsumedEvents.FilesRemoved(uploaded.Id);
        await Fixture.ConsumedEvents.CdnPurged(uploaded.Id);
    }

    [Fact]
    public async Task DeleteImage_ImageThatWasAlreadyDeleted_ReturnsNotFound()
    {
        var tenantId = NewTenantId();
        var uploaded = await Api.Upload(TestImages.Jpeg800X600, tenantId);
        await Api.Delete(uploaded.Id, tenantId);

        var response = await Api.SendDelete(uploaded.Id, tenantId);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteImage_IdThatWasNeverUploaded_ReturnsNotFound()
    {
        var unknownId = Guid.NewGuid().ToString("N");

        var response = await Api.SendDelete(unknownId, NewTenantId());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(unknownId, await response.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// A blank id is whitespace rather than an empty segment: with the id in the route, an empty one does
    /// not match the pattern at all and never reaches the validator (see the test below).
    /// </summary>
    [Fact]
    public async Task DeleteImage_BlankImageId_ReturnsValidationProblem()
    {
        var problem = await ReadValidationProblem(await Api.SendDelete(" ", NewTenantId()));

        Assert.Contains("ImageId", problem.Errors.Keys);
    }

    [Fact]
    public async Task DeleteImage_WithoutAnImageId_ReturnsNotFound()
    {
        var response = await Api.SendDelete(string.Empty, NewTenantId());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteImage_NoTenantHeader_ReturnsValidationProblem()
    {
        var problem = await ReadValidationProblem(
            await Api.SendDelete(Guid.NewGuid().ToString("N"), tenantId: null));

        Assert.Contains("TenantId", problem.Errors.Keys);
    }
}
