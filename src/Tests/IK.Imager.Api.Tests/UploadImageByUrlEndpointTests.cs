using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace IK.Imager.Api.Tests;

/// <summary>
/// POST /Images/UploadByUrl.
///
/// The url the service is asked to download from is the public blob url of an image uploaded a moment
/// earlier: it is a real http address the host really fetches over the network, and it needs no fixture of
/// its own because Azurite is already serving it.
/// </summary>
[Trait("Category", "Integration")]
public class UploadImageByUrlEndpointTests(ImagerApiFixture fixture) : ImagerApiTests(fixture)
{
    [Fact]
    public async Task UploadByUrl_UrlOfAnImageTheServiceCanReach_StoresItAsANewImage()
    {
        var imageGroup = NewImageGroup();
        var source = await Api.Upload(TestImages.Jpeg1200X900, imageGroup);

        var copy = await Api.UploadByUrl(source.Url, imageGroup);

        Assert.NotEqual(source.Id, copy.Id);
        Assert.NotEqual(source.Url, copy.Url);
        //same bytes downloaded, so the same image is described
        Assert.Equal(source.Hash, copy.Hash);
        Assert.Equal(source.Bytes, copy.Bytes);
        Assert.Equal(1200, copy.Width);
        Assert.Equal(900, copy.Height);
        Assert.Equal("image/jpeg", copy.MimeType);
    }

    [Fact]
    public async Task UploadByUrl_UrlOfAnImageTheServiceCanReach_MakesTheCopyAvailableForLookup()
    {
        var imageGroup = NewImageGroup();
        var source = await Api.Upload(TestImages.Jpeg800X600, imageGroup);

        var copy = await Api.UploadByUrl(source.Url, imageGroup);
        var found = await Api.LookupSingle(copy.Id, imageGroup);

        Assert.Equal(copy.Id, found.Id);
    }

    [Fact]
    public async Task UploadByUrl_UrlThatIsNotWellFormed_ReturnsValidationProblem()
    {
        var response = await Api.PostUploadByUrl(new { ImageUrl = "not a url", ImageGroup = NewImageGroup() });

        var problem = await ReadValidationProblem(response);

        Assert.Contains("ImageUrl", problem.Errors.Keys);
    }

    [Fact]
    public async Task UploadByUrl_RelativeUrl_ReturnsValidationProblem()
    {
        var response = await Api.PostUploadByUrl(new { ImageUrl = "/images/photo.jpg", ImageGroup = NewImageGroup() });

        var problem = await ReadValidationProblem(response);

        Assert.Contains("ImageUrl", problem.Errors.Keys);
    }

    [Fact]
    public async Task UploadByUrl_EmptyUrl_ReturnsValidationProblem()
    {
        var response = await Api.PostUploadByUrl(new { ImageUrl = "", ImageGroup = NewImageGroup() });

        var problem = await ReadValidationProblem(response);

        Assert.Contains("ImageUrl", problem.Errors.Keys);
    }

    [Fact]
    public async Task UploadByUrl_ImageGroupShorterThanTheMinimum_ReturnsValidationProblem()
    {
        var response = await Api.PostUploadByUrl(new { ImageUrl = "https://example.com/photo.jpg", ImageGroup = "ab" });

        var problem = await ReadValidationProblem(response);

        Assert.Contains("ImageGroup", problem.Errors.Keys);
    }

    /// <summary>
    /// A well formed url passes the request validator, so a url nothing answers on only fails further in:
    /// ImageDownloadClient returns null and ImageUploader rejects it. That is the caller's error rather than
    /// a fault, so it is a 400 - which is what this endpoint documents for a url no image is found by.
    /// </summary>
    [Fact]
    public async Task UploadByUrl_UrlNothingIsListeningOn_ReturnsBadRequest()
    {
        var response = await Api.PostUploadByUrl(new
        {
            ImageUrl = "http://localhost:1/missing.jpg",
            ImageGroup = NewImageGroup()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadByUrl_UrlThatAnswersNotFound_ReturnsBadRequest()
    {
        //a reachable host is a different path from a refused connection: blob storage answers, with a 404
        var imageGroup = NewImageGroup();
        var source = await Api.Upload(TestImages.Jpeg800X600, imageGroup);
        var missingBlobUrl = source.Url.Replace(".jpg", "-does-not-exist.jpg");

        var response = await Api.PostUploadByUrl(new { ImageUrl = missingBlobUrl, ImageGroup = imageGroup });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
