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
        var imageGroup = NewImageGroup();
        var uploaded = await Api.Upload(TestImages.Jpeg800X600, imageGroup);

        var response = await Api.SendDelete(uploaded.Id, imageGroup);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteImage_UploadedImage_RemovesItFromLookupImmediately()
    {
        var imageGroup = NewImageGroup();
        var uploaded = await Api.Upload(TestImages.Jpeg800X600, imageGroup);

        await Api.Delete(uploaded.Id, imageGroup);
        var result = await Api.Lookup([uploaded.Id], imageGroup);

        Assert.Empty(result.Images);
    }

    [Fact]
    public async Task DeleteImage_WithoutAnImageGroup_StillDeletesTheImage()
    {
        var imageGroup = NewImageGroup();
        var uploaded = await Api.Upload(TestImages.Jpeg800X600, imageGroup);

        await Api.Delete(uploaded.Id, imageGroup: null);

        Assert.Empty((await Api.Lookup([uploaded.Id], imageGroup)).Images);
    }

    [Fact]
    public async Task DeleteImage_ImageWithThumbnails_RemovesTheFilesWithoutFaulting()
    {
        var imageGroup = NewImageGroup();
        var uploaded = await Api.Upload(TestImages.Jpeg1200X900, imageGroup);
        await Fixture.ConsumedEvents.ThumbnailsGenerated(uploaded.Id);

        await Api.Delete(uploaded.Id, imageGroup);

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
        var imageGroup = NewImageGroup();
        var uploaded = await Api.Upload(TestImages.Jpeg1200X900, imageGroup);
        await Fixture.ConsumedEvents.ThumbnailsGenerated(uploaded.Id);

        await Api.Delete(uploaded.Id, imageGroup);

        await Fixture.ConsumedEvents.FilesRemoved(uploaded.Id);
        await Fixture.ConsumedEvents.CdnPurged(uploaded.Id);
    }

    [Fact]
    public async Task DeleteImage_ImageThatWasAlreadyDeleted_ReturnsNotFound()
    {
        var imageGroup = NewImageGroup();
        var uploaded = await Api.Upload(TestImages.Jpeg800X600, imageGroup);
        await Api.Delete(uploaded.Id, imageGroup);

        var response = await Api.SendDelete(uploaded.Id, imageGroup);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteImage_IdThatWasNeverUploaded_ReturnsNotFound()
    {
        var unknownId = Guid.NewGuid().ToString("N");

        var response = await Api.SendDelete(unknownId, NewImageGroup());

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
        var problem = await ReadValidationProblem(await Api.SendDelete(" ", NewImageGroup()));

        Assert.Contains("ImageId", problem.Errors.Keys);
    }

    [Fact]
    public async Task DeleteImage_WithoutAnImageId_ReturnsNotFound()
    {
        var response = await Api.SendDelete(string.Empty, NewImageGroup());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteImage_ImageGroupShorterThanTheMinimum_ReturnsValidationProblem()
    {
        var problem = await ReadValidationProblem(
            await Api.SendDelete(Guid.NewGuid().ToString("N"), "ab"));

        Assert.Contains("ImageGroup", problem.Errors.Keys);
    }
}
