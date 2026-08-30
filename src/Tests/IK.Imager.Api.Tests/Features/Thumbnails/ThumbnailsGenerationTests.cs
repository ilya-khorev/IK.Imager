using System;
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
/// CreateThumbnailsConsumer consumes it, and ThumbnailGenerator resizes the original and upserts the
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
        var tenantId = NewTenantId();
        var uploaded = await Api.Upload(TestImages.Jpeg1200X900, tenantId);

        await Fixture.ConsumedEvents.ThumbnailsGenerated(uploaded.Id);
        var found = await Api.LookupSingle(uploaded.Id, tenantId);

        //smallest first, and 1200x900 scales to 200x150, 400x300 and 1000x750
        Assert.Equal([(200, 150), (400, 300), (1000, 750)],
            found.Thumbnails.Select(thumbnail => (thumbnail.Width, thumbnail.Height)));
    }

    /// <summary>
    /// A thumbnail path is derived from its original's, so it inherits the tenant, the collection and the
    /// unique prefix without being told any of them - and carries the width it was resized to.
    /// </summary>
    [Fact]
    public async Task Generate_OriginalWithCollectionAndPrefix_ThumbnailsInheritTheWholePath()
    {
        var tenantId = NewTenantId();
        var collection = NewCollection();
        var uploaded = await Api.Upload(TestImages.Jpeg1200X900, tenantId, collection, "sku-9",
            includeCollectionInPath: true, addUniquePrefix: true);

        await Fixture.ConsumedEvents.ThumbnailsGenerated(uploaded.Id);
        var found = await Api.LookupSingle(uploaded.Id, tenantId);

        //thumbnails live in their own container, so only the path from the tenant segment on is shared
        var relative = uploaded.Url[uploaded.Url.IndexOf($"/{tenantId}/", StringComparison.Ordinal)..];
        var stem = relative[..^".jpg".Length];

        Assert.StartsWith($"/{tenantId}/{collection}/", relative);
        Assert.Equal([$"{stem}_200.jpg", $"{stem}_400.jpg", $"{stem}_1000.jpg"],
            found.Thumbnails.Select(thumbnail => thumbnail.Url[thumbnail.Url.IndexOf($"/{tenantId}/", StringComparison.Ordinal)..]));
    }

    [Fact]
    public async Task Generate_PlainOriginal_ThumbnailUrlIsTheOriginalPlusItsWidth()
    {
        var tenantId = NewTenantId();
        var uploaded = await Api.Upload(TestImages.Jpeg800X600, tenantId, imageId: "sku-8");

        await Fixture.ConsumedEvents.ThumbnailsGenerated(uploaded.Id);
        var found = await Api.LookupSingle(uploaded.Id, tenantId);

        Assert.All(found.Thumbnails, thumbnail => Assert.EndsWith($"/{tenantId}/sku-8_{thumbnail.Width}.jpg",
            thumbnail.Url));
    }

    [Fact]
    public async Task Generate_ImageNarrowerThanTheWidestTargetWidth_SkipsThatTargetOnly()
    {
        var tenantId = NewTenantId();
        var uploaded = await Api.Upload(TestImages.Jpeg800X600, tenantId);

        await Fixture.ConsumedEvents.ThumbnailsGenerated(uploaded.Id);
        var found = await Api.LookupSingle(uploaded.Id, tenantId);

        //1000 is wider than the 800px original, so only 200 and 400 are generated
        Assert.Equal([(200, 150), (400, 300)],
            found.Thumbnails.Select(thumbnail => (thumbnail.Width, thumbnail.Height)));
    }

    [Fact]
    public async Task Generate_ImageExactlyAsWideAsATargetWidth_DoesNotProduceThatThumbnail()
    {
        var tenantId = NewTenantId();
        var uploaded = await Api.Upload(TestImages.Png1000X1000, tenantId);

        await Fixture.ConsumedEvents.ThumbnailsGenerated(uploaded.Id);
        var found = await Api.LookupSingle(uploaded.Id, tenantId);

        //a thumbnail as wide as the original is not a thumbnail - the 1000 target is dropped
        Assert.Equal([(200, 200), (400, 400)],
            found.Thumbnails.Select(thumbnail => (thumbnail.Width, thumbnail.Height)));
    }

    [Fact]
    public async Task Generate_ImageNoWiderThanTheNarrowestTargetWidth_ProducesNoThumbnails()
    {
        var tenantId = NewTenantId();
        var uploaded = await Api.Upload(TestImages.Gif200X200, tenantId);

        //the generator gives up before resizing anything, but it still consumes the event - which is what
        //makes "nothing was generated" assertable rather than a race
        await Fixture.ConsumedEvents.ThumbnailsGenerated(uploaded.Id);
        var found = await Api.LookupSingle(uploaded.Id, tenantId);

        Assert.Empty(found.Thumbnails);
    }

    [Fact]
    public async Task Generate_Thumbnails_AreStoredAsDistinctImagesOfTheOriginalsMimeType()
    {
        var tenantId = NewTenantId();
        var uploaded = await Api.Upload(TestImages.Jpeg1200X900, tenantId);

        await Fixture.ConsumedEvents.ThumbnailsGenerated(uploaded.Id);
        var found = await Api.LookupSingle(uploaded.Id, tenantId);

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
        var tenantId = NewTenantId();
        var uploaded = await Api.Upload(TestImages.Jpeg1200X900, tenantId);

        await Fixture.ConsumedEvents.ThumbnailsGenerated(uploaded.Id);
        var found = await Api.LookupSingle(uploaded.Id, tenantId);

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
        var tenantId = NewTenantId();
        var source = await Api.Upload(TestImages.Jpeg1200X900, tenantId);

        var copy = await Api.UploadByUrl(source.Url, tenantId);

        await Fixture.ConsumedEvents.ThumbnailsGenerated(copy.Id);
        var found = await Api.LookupSingle(copy.Id, tenantId);

        Assert.Equal(3, found.Thumbnails.Count);
    }

    /// <summary>
    /// An upload may name the widths it wants instead of taking the configured [200, 400, 1000]. The
    /// widths travel on the image metadata rather than on the integration event, so this is also what
    /// proves they survive the hop from the upload to the consumer that thumbnails it.
    /// </summary>
    [Fact]
    public async Task Generate_UploadNamedItsOwnWidths_UsesThemInsteadOfTheConfiguredOnes()
    {
        var tenantId = NewTenantId();
        var uploaded = await Api.Upload(TestImages.Jpeg1200X900, tenantId, thumbnailTargetWidths: [300, 600]);

        await Fixture.ConsumedEvents.ThumbnailsGenerated(uploaded.Id);
        var found = await Api.LookupSingle(uploaded.Id, tenantId);

        //none of the configured widths appear - 1200x900 scales to 300x225 and 600x450
        Assert.Equal([(300, 225), (600, 450)],
            found.Thumbnails.Select(thumbnail => (thumbnail.Width, thumbnail.Height)));
    }

    [Fact]
    public async Task Generate_UploadNamedOneWidth_ProducesThatThumbnailAlone()
    {
        var tenantId = NewTenantId();
        var uploaded = await Api.Upload(TestImages.Jpeg800X600, tenantId, imageId: "sku-w",
            thumbnailTargetWidths: [500]);

        await Fixture.ConsumedEvents.ThumbnailsGenerated(uploaded.Id);
        var found = await Api.LookupSingle(uploaded.Id, tenantId);

        var thumbnail = Assert.Single(found.Thumbnails);
        Assert.Equal((500, 375), (thumbnail.Width, thumbnail.Height));
        Assert.EndsWith($"/{tenantId}/sku-w_500.jpg", thumbnail.Url);
    }

    [Fact]
    public async Task Generate_UploadNamedAWidthWiderThanTheImage_SkipsThatWidthOnly()
    {
        var tenantId = NewTenantId();
        var uploaded = await Api.Upload(TestImages.Jpeg800X600, tenantId, thumbnailTargetWidths: [400, 1500]);

        await Fixture.ConsumedEvents.ThumbnailsGenerated(uploaded.Id);
        var found = await Api.LookupSingle(uploaded.Id, tenantId);

        //a width the image does not reach produces no thumbnail rather than failing the upload
        Assert.Equal([(400, 300)],
            found.Thumbnails.Select(thumbnail => (thumbnail.Width, thumbnail.Height)));
    }

    [Fact]
    public async Task Generate_UploadNamedWidthsAllWiderThanTheImage_ProducesNoThumbnails()
    {
        var tenantId = NewTenantId();
        var uploaded = await Api.Upload(TestImages.Jpeg800X600, tenantId, thumbnailTargetWidths: [1200]);

        await Fixture.ConsumedEvents.ThumbnailsGenerated(uploaded.Id);
        var found = await Api.LookupSingle(uploaded.Id, tenantId);

        //the configured 200 and 400 would both have produced one, so this only holds if the named widths won
        Assert.Empty(found.Thumbnails);
    }

    [Fact]
    public async Task Generate_UploadByUrlNamedItsOwnWidths_UsesThemForTheCopy()
    {
        var tenantId = NewTenantId();
        var source = await Api.Upload(TestImages.Jpeg1200X900, tenantId);

        var copy = await Api.UploadByUrl(source.Url, tenantId, thumbnailTargetWidths: [600]);

        await Fixture.ConsumedEvents.ThumbnailsGenerated(copy.Id);
        var found = await Api.LookupSingle(copy.Id, tenantId);

        Assert.Equal([(600, 450)],
            found.Thumbnails.Select(thumbnail => (thumbnail.Width, thumbnail.Height)));
    }
}
