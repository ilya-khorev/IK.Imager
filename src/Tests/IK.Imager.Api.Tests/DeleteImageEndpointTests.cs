using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace IK.Imager.Api.Tests;

/// <summary>
/// DELETE /Images.
///
/// The call removes the metadata only, which is what makes the image vanish from lookups straight away;
/// the blobs go afterwards, when RemoveImageFilesHandler consumes the event.
/// </summary>
[Trait("Category", "Integration")]
public class DeleteImageEndpointTests(ImagerApiFixture fixture) : ImagerApiTests(fixture)
{
    [Fact]
    public async Task DeleteImage_UploadedImage_ReturnsNoContent()
    {
        var imageGroup = NewImageGroup();
        var uploaded = await Api.Upload(TestImages.Jpeg800X600, imageGroup);

        var response = await Api.SendDelete(new { ImageId = uploaded.Id, ImageGroup = imageGroup });

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

    [Fact]
    public async Task DeleteImage_ImageThatWasAlreadyDeleted_ReturnsNotFound()
    {
        var imageGroup = NewImageGroup();
        var uploaded = await Api.Upload(TestImages.Jpeg800X600, imageGroup);
        await Api.Delete(uploaded.Id, imageGroup);

        var response = await Api.SendDelete(new { ImageId = uploaded.Id, ImageGroup = imageGroup });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteImage_IdThatWasNeverUploaded_ReturnsNotFound()
    {
        var unknownId = Guid.NewGuid().ToString("N");

        var response = await Api.SendDelete(new { ImageId = unknownId, ImageGroup = NewImageGroup() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(unknownId, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DeleteImage_WithoutAnImageId_ReturnsValidationProblem()
    {
        var problem = await ReadValidationProblem(
            await Api.SendDelete(new { ImageId = "", ImageGroup = NewImageGroup() }));

        Assert.Contains("ImageId", problem.Errors.Keys);
    }

    [Fact]
    public async Task DeleteImage_ImageGroupShorterThanTheMinimum_ReturnsValidationProblem()
    {
        var problem = await ReadValidationProblem(
            await Api.SendDelete(new { ImageId = Guid.NewGuid().ToString("N"), ImageGroup = "ab" }));

        Assert.Contains("ImageGroup", problem.Errors.Keys);
    }
}
