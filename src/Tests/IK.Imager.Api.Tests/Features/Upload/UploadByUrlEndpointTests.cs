using System.Net;
using System.Threading.Tasks;
using IK.Imager.Api.Tests.Infrastructure;
using Xunit;

namespace IK.Imager.Api.Tests.Features.Upload;

/// <summary>
/// POST /images/upload-by-url.
///
/// The url the service is asked to download from is the public blob url of an image uploaded a moment
/// earlier: it is a real http address the host really fetches over the network, and it needs no fixture of
/// its own because Azurite is already serving it.
/// </summary>
[Trait("Category", "Integration")]
public class UploadByUrlEndpointTests(ImagerApiFixture fixture) : ImagerApiTests(fixture)
{
    [Fact]
    public async Task UploadByUrl_UrlOfAnImageTheServiceCanReach_StoresItAsANewImage()
    {
        var tenantId = NewTenantId();
        var source = await Api.Upload(TestImages.Jpeg1200X900, tenantId);

        var copy = await Api.UploadByUrl(source.Url, tenantId);

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
        var tenantId = NewTenantId();
        var source = await Api.Upload(TestImages.Jpeg800X600, tenantId);

        var copy = await Api.UploadByUrl(source.Url, tenantId);
        var found = await Api.LookupSingle(copy.Id, tenantId);

        Assert.Equal(copy.Id, found.Id);
    }

    [Fact]
    public async Task UploadByUrl_UrlThatIsNotWellFormed_ReturnsValidationProblem()
    {
        var response = await Api.PostUploadByUrl(new { ImageUrl = "not a url" }, NewTenantId());

        var problem = await ReadValidationProblem(response);

        Assert.Contains("ImageUrl", problem.Errors.Keys);
    }

    [Fact]
    public async Task UploadByUrl_RelativeUrl_ReturnsValidationProblem()
    {
        var response = await Api.PostUploadByUrl(new { ImageUrl = "/images/photo.jpg" }, NewTenantId());

        var problem = await ReadValidationProblem(response);

        Assert.Contains("ImageUrl", problem.Errors.Keys);
    }

    [Fact]
    public async Task UploadByUrl_EmptyUrl_ReturnsValidationProblem()
    {
        var response = await Api.PostUploadByUrl(new { ImageUrl = "" }, NewTenantId());

        var problem = await ReadValidationProblem(response);

        Assert.Contains("ImageUrl", problem.Errors.Keys);
    }

    [Fact]
    public async Task UploadByUrl_NoTenantHeader_ReturnsValidationProblem()
    {
        var response = await Api.PostUploadByUrl(new { ImageUrl = "https://example.com/photo.jpg" }, tenantId: null);

        var problem = await ReadValidationProblem(response);

        Assert.Contains("TenantId", problem.Errors.Keys);
    }

    [Fact]
    public async Task UploadByUrl_TenantThatIsNotWellFormed_ReturnsValidationProblem()
    {
        var response = await Api.PostUploadByUrl(new { ImageUrl = "https://example.com/photo.jpg" }, "Not A Tenant");

        var problem = await ReadValidationProblem(response);

        Assert.Contains("TenantId", problem.Errors.Keys);
    }

    /// <summary>
    /// A well formed url passes the request validator, so a url nothing answers on only fails further in:
    /// ImageDownloader returns null and ImageUploader rejects it. That is the caller's error rather than
    /// a fault, so it is a 400 - which is what this endpoint documents for a url no image is found by.
    /// </summary>
    [Fact]
    public async Task UploadByUrl_UrlNothingIsListeningOn_ReturnsBadRequest()
    {
        var response = await Api.PostUploadByUrl(new { ImageUrl = "http://localhost:1/missing.jpg" }, NewTenantId());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadByUrl_UrlThatAnswersNotFound_ReturnsBadRequest()
    {
        //a reachable host is a different path from a refused connection: blob storage answers, with a 404
        var tenantId = NewTenantId();
        var source = await Api.Upload(TestImages.Jpeg800X600, tenantId);
        var missingBlobUrl = source.Url.Replace(".jpg", "-does-not-exist.jpg");

        var response = await Api.PostUploadByUrl(new { ImageUrl = missingBlobUrl }, tenantId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
