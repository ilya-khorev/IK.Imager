using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace IK.Imager.Api.Tests;

/// <summary>
/// POST /Images/Lookup - fetching images by id. There is no querying or filtering, hence "lookup".
/// </summary>
[Trait("Category", "Integration")]
public class LookupImagesEndpointTests(ImagerApiFixture fixture) : ImagerApiTests(fixture)
{
    [Fact]
    public async Task LookupByIds_ImageGroupOfTheUploadedImage_ReturnsItsFullMetadata()
    {
        var imageGroup = NewImageGroup();
        var uploaded = await Api.Upload(TestImages.Jpeg1200X900, imageGroup);

        var found = await Api.LookupSingle(uploaded.Id, imageGroup);

        Assert.Equal(uploaded.Id, found.Id);
        Assert.Equal(uploaded.Url, found.Url);
        Assert.Equal(uploaded.Hash, found.Hash);
        Assert.Equal(uploaded.Bytes, found.Bytes);
        Assert.Equal(uploaded.Width, found.Width);
        Assert.Equal(uploaded.Height, found.Height);
        Assert.Equal(uploaded.MimeType, found.MimeType);
    }

    [Fact]
    public async Task LookupByIds_WithoutAnImageGroup_StillFindsTheImage()
    {
        //the image group is the partition key, so leaving it out makes this a cross-partition read - it is
        //documented as optional-but-slower rather than required
        var uploaded = await Api.Upload(TestImages.Jpeg800X600, NewImageGroup());

        var found = await Api.LookupSingle(uploaded.Id, imageGroup: null);

        Assert.Equal(uploaded.Id, found.Id);
    }

    [Fact]
    public async Task LookupByIds_SeveralIds_ReturnsEveryOneOfThem()
    {
        var imageGroup = NewImageGroup();
        var first = await Api.Upload(TestImages.Jpeg1200X900, imageGroup);
        var second = await Api.Upload(TestImages.Jpeg800X600, imageGroup);
        var third = await Api.Upload(TestImages.Png1000X1000, imageGroup);

        var result = await Api.Lookup([first.Id, second.Id, third.Id], imageGroup);

        Assert.Equal(3, result.Images.Count);
        //sorted on both sides: the repository returns whatever order the Cosmos query yields
        Assert.Equal(
            new[] { first.Id, second.Id, third.Id }.OrderBy(id => id),
            result.Images.Select(image => image.Id).OrderBy(id => id));
    }

    [Fact]
    public async Task LookupByIds_IdThatWasNeverUploaded_ReturnsAnEmptyResult()
    {
        var result = await Api.Lookup([Guid.NewGuid().ToString("N")], NewImageGroup());

        Assert.Empty(result.Images);
    }

    [Fact]
    public async Task LookupByIds_ImageGroupOfAnotherTest_DoesNotReturnTheImage()
    {
        var uploaded = await Api.Upload(TestImages.Jpeg800X600, NewImageGroup());

        var result = await Api.Lookup([uploaded.Id], NewImageGroup());

        Assert.Empty(result.Images);
    }

    [Fact]
    public async Task LookupByIds_MoreThanTwoHundredIds_ReturnsValidationProblem()
    {
        var tooManyIds = Enumerable.Range(0, 201).Select(_ => Guid.NewGuid().ToString("N")).ToArray();

        var problem = await ReadValidationProblem(
            await Api.PostLookup(new { ImageIds = tooManyIds, ImageGroup = NewImageGroup() }));

        Assert.Contains("ImageIds", problem.Errors.Keys);
    }

    [Fact]
    public async Task LookupByIds_ExactlyTwoHundredIds_IsAccepted()
    {
        var maximumIds = Enumerable.Range(0, 200).Select(_ => Guid.NewGuid().ToString("N")).ToArray();

        var result = await Api.Lookup(maximumIds, NewImageGroup());

        Assert.Empty(result.Images);
    }

    [Fact]
    public async Task LookupByIds_EmptyIdArray_ReturnsValidationProblem()
    {
        var problem = await ReadValidationProblem(
            await Api.PostLookup(new { ImageIds = Array.Empty<string>(), ImageGroup = NewImageGroup() }));

        Assert.Contains("ImageIds", problem.Errors.Keys);
    }

    [Fact]
    public async Task LookupByIds_ImageGroupShorterThanTheMinimum_ReturnsValidationProblem()
    {
        var problem = await ReadValidationProblem(
            await Api.PostLookup(new { ImageIds = new[] { Guid.NewGuid().ToString("N") }, ImageGroup = "ab" }));

        Assert.Contains("ImageGroup", problem.Errors.Keys);
    }
}
