using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using IK.Imager.Api.Contract;
using IK.Imager.Api.Tests.Infrastructure;
using Xunit;

namespace IK.Imager.Api.Tests.Features.Thumbnails;

/// <summary>
/// The asynchronous half of an upload: OriginalImageUploadedIntegrationEvent goes onto the bus,
/// CreateThumbnailsHandler consumes it, and ThumbnailGenerator resizes the original and upserts the
/// thumbnails onto the metadata document. The tests wait on the consumer rather than on the clock -
/// see <see cref="ConsumedEventObserver"/>.
///
/// Thumbnails:TargetWidth is [200, 400, 1000], and a thumbnail is only produced for a target strictly
/// narrower than the original, so the expected counts below follow from each sample image's width.
/// Each thumbnail is resized from the previous one, which is why the heights step down proportionally.
/// </summary>
[Trait("Category", "Integration")]
public class ThumbnailsGenerationTests(ImagerApiFixture fixture) : ImagerApiTests(fixture)
{
    [Fact]
    public async Task Generate_ImageWiderThanEveryTargetWidth_ProducesAThumbnailPerTarget()
    {
        var imageGroup = NewImageGroup();
        var uploaded = await Api.Upload(TestImages.Jpeg1200X900, imageGroup);

        await Fixture.ConsumedEvents.ThumbnailsGenerated(uploaded.Id);
        var found = await Api.LookupSingle(uploaded.Id, imageGroup);

        //smallest first, and 1200x900 scales to 200x150, 400x300 and 1000x750
        Assert.Equal([(200, 150), (400, 300), (1000, 750)],
            found.Thumbnails.Select(thumbnail => (thumbnail.Width, thumbnail.Height)));
    }

    [Fact]
    public async Task Generate_ImageNarrowerThanTheWidestTargetWidth_SkipsThatTargetOnly()
    {
        var imageGroup = NewImageGroup();
        var uploaded = await Api.Upload(TestImages.Jpeg800X600, imageGroup);

        await Fixture.ConsumedEvents.ThumbnailsGenerated(uploaded.Id);
        var found = await Api.LookupSingle(uploaded.Id, imageGroup);

        //1000 is wider than the 800px original, so only 200 and 400 are generated
        Assert.Equal([(200, 150), (400, 300)],
            found.Thumbnails.Select(thumbnail => (thumbnail.Width, thumbnail.Height)));
    }

    [Fact]
    public async Task Generate_ImageExactlyAsWideAsATargetWidth_DoesNotProduceThatThumbnail()
    {
        var imageGroup = NewImageGroup();
        var uploaded = await Api.Upload(TestImages.Png1000X1000, imageGroup);

        await Fixture.ConsumedEvents.ThumbnailsGenerated(uploaded.Id);
        var found = await Api.LookupSingle(uploaded.Id, imageGroup);

        //a thumbnail as wide as the original is not a thumbnail - the 1000 target is dropped
        Assert.Equal([(200, 200), (400, 400)],
            found.Thumbnails.Select(thumbnail => (thumbnail.Width, thumbnail.Height)));
    }

    [Fact]
    public async Task Generate_ImageNoWiderThanTheNarrowestTargetWidth_ProducesNoThumbnails()
    {
        var imageGroup = NewImageGroup();
        var uploaded = await Api.Upload(TestImages.Gif200X200, imageGroup);

        //the generator gives up before resizing anything, but it still consumes the event - which is what
        //makes "nothing was generated" assertable rather than a race
        await Fixture.ConsumedEvents.ThumbnailsGenerated(uploaded.Id);
        var found = await Api.LookupSingle(uploaded.Id, imageGroup);

        Assert.Empty(found.Thumbnails);
    }

    [Fact]
    public async Task Generate_Thumbnails_AreStoredAsDistinctImagesOfTheOriginalsMimeType()
    {
        var imageGroup = NewImageGroup();
        var uploaded = await Api.Upload(TestImages.Jpeg1200X900, imageGroup);

        await Fixture.ConsumedEvents.ThumbnailsGenerated(uploaded.Id);
        var found = await Api.LookupSingle(uploaded.Id, imageGroup);

        Assert.All(found.Thumbnails, thumbnail =>
        {
            Assert.NotEmpty(thumbnail.Id);
            Assert.NotEmpty(thumbnail.Hash);
            Assert.Equal("image/jpeg", thumbnail.MimeType);
            Assert.True(thumbnail.Bytes > 0, "A generated thumbnail should report a size.");
        });

        var identifiers = found.Thumbnails.Select(thumbnail => thumbnail.Id).ToList();
        Assert.Equal(identifiers.Count, identifiers.Distinct().Count());
        Assert.DoesNotContain(uploaded.Id, identifiers);
    }

    [Fact]
    public async Task Generate_Thumbnails_AreReachableByTheUrlTheLookupReturns()
    {
        var imageGroup = NewImageGroup();
        var uploaded = await Api.Upload(TestImages.Jpeg1200X900, imageGroup);

        await Fixture.ConsumedEvents.ThumbnailsGenerated(uploaded.Id);
        var found = await Api.LookupSingle(uploaded.Id, imageGroup);

        //the thumbnails live in their own public blob container, so the url is enough - no client here
        using var anonymousClient = new HttpClient();
        foreach (ImageInfo thumbnail in found.Thumbnails)
        {
            var stored = await anonymousClient.GetAsync(thumbnail.Url);

            Assert.Equal(HttpStatusCode.OK, stored.StatusCode);
            Assert.Equal(thumbnail.Bytes, (await stored.Content.ReadAsByteArrayAsync()).Length);
        }
    }

    [Fact]
    public async Task Generate_UploadByUrl_GeneratesThumbnailsForTheCopyToo()
    {
        var imageGroup = NewImageGroup();
        var source = await Api.Upload(TestImages.Jpeg1200X900, imageGroup);

        var copy = await Api.UploadByUrl(source.Url, imageGroup);

        await Fixture.ConsumedEvents.ThumbnailsGenerated(copy.Id);
        var found = await Api.LookupSingle(copy.Id, imageGroup);

        Assert.Equal(3, found.Thumbnails.Count);
    }
}
