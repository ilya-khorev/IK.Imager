using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace IK.Imager.Api.Tests;

/// <summary>
/// POST /Images/Upload - the multipart form endpoint.
/// </summary>
[Trait("Category", "Integration")]
public class UploadImageEndpointTests(ImagerApiFixture fixture) : ImagerApiTests(fixture)
{
    [Fact]
    public async Task Upload_JpegFile_ReturnsInfoAboutTheStoredImage()
    {
        var imageGroup = NewImageGroup();

        var image = await Api.Upload(TestImages.Jpeg1200X900, imageGroup);

        Assert.NotEmpty(image.Id);
        Assert.NotEmpty(image.Hash);
        Assert.Equal(1200, image.Width);
        Assert.Equal(900, image.Height);
        Assert.Equal("image/jpeg", image.MimeType);
        Assert.True(image.Bytes > 0, "The stored image should report a size.");
        Assert.InRange(image.DateAdded, DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddMinutes(5));
        Assert.True(Uri.IsWellFormedUriString(image.Url, UriKind.Absolute), $"'{image.Url}' is not an absolute url.");
    }

    [Fact]
    public async Task Upload_PngFile_ReportsThePngMimeType()
    {
        var image = await Api.Upload(TestImages.Png1000X1000, NewImageGroup());

        Assert.Equal("image/png", image.MimeType);
        Assert.Equal(1000, image.Width);
        Assert.Equal(1000, image.Height);
    }

    [Fact]
    public async Task Upload_JpegFile_StoresBytesThatAreReachableByTheReturnedUrl()
    {
        var image = await Api.Upload(TestImages.Jpeg1200X900, NewImageGroup());

        //the blob container is created with public blob access, so the url alone is enough - which is the
        //whole point of returning it to the client
        using var anonymousClient = new System.Net.Http.HttpClient();
        var stored = await anonymousClient.GetAsync(image.Url);

        Assert.Equal(HttpStatusCode.OK, stored.StatusCode);
        Assert.Equal(image.Bytes, (await stored.Content.ReadAsByteArrayAsync()).Length);
    }

    [Fact]
    public async Task Upload_JpegFile_MakesTheImageAvailableForLookupImmediately()
    {
        var imageGroup = NewImageGroup();

        var image = await Api.Upload(TestImages.Jpeg1200X900, imageGroup);
        var found = await Api.LookupSingle(image.Id, imageGroup);

        Assert.Equal(image.Id, found.Id);
        Assert.Equal(image.Hash, found.Hash);
        Assert.Equal(image.Width, found.Width);
        Assert.Equal(image.Height, found.Height);
    }

    [Fact]
    public async Task Upload_FileThatIsNotAnImage_ReturnsBadRequest()
    {
        var response = await Api.PostUpload(TestImages.NotAnImage, NewImageGroup());

        //ImageValidator cannot detect a format, the core throws, and GlobalExceptionHandler maps it to a 400
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_ImageGroupShorterThanTheMinimum_ReturnsValidationProblem()
    {
        var problem = await ReadValidationProblem(await Api.PostUpload(TestImages.Jpeg800X600, "ab"));

        Assert.Contains("ImageGroup", problem.Errors.Keys);
    }

    [Fact]
    public async Task Upload_ImageGroupLongerThanTheMaximum_ReturnsValidationProblem()
    {
        var problem = await ReadValidationProblem(await Api.PostUpload(TestImages.Jpeg800X600, new string('g', 31)));

        Assert.Contains("ImageGroup", problem.Errors.Keys);
    }

    [Fact]
    public async Task Upload_WithoutAnImageGroup_ReturnsValidationProblem()
    {
        var problem = await ReadValidationProblem(await Api.PostUpload(TestImages.Jpeg800X600, imageGroup: null));

        Assert.Contains("ImageGroup", problem.Errors.Keys);
    }
}
