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
    /// Pins today's behaviour, which is not the documented one.
    ///
    /// A well formed url passes the request validator, so an unreachable one only fails further in:
    /// ImageDownloadClient returns null, and ImageUploader.UploadByUrl passes that null straight on - see
    /// the '//todo handle' there. ImageMetadataReader then rejects the null stream with an
    /// ArgumentNullException, which GlobalExceptionHandler can only read as a 500, while the endpoint's own
    /// documentation promises a 400 "if the image is not found by the given image url".
    ///
    /// So this test should start failing the day that todo is addressed - at which point the assertion
    /// becomes BadRequest.
    /// </summary>
    [Fact]
    public async Task UploadByUrl_UrlNothingIsServing_CurrentlyReturnsServerError()
    {
        var response = await Api.PostUploadByUrl(new
        {
            ImageUrl = "http://localhost:1/missing.jpg",
            ImageGroup = NewImageGroup()
        });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
