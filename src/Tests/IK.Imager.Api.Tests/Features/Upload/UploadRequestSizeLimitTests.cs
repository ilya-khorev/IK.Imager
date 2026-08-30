using System.Linq;
using IK.Imager.Api.Tests.Infrastructure;
using IK.Imager.Core.Upload;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace IK.Imager.Api.Tests.Features.Upload;

/// <summary>
/// Both upload routes bound their request body, so an oversized one is refused as it arrives rather than
/// after the whole of it has been buffered.
///
/// Asserted as endpoint metadata rather than by posting an oversized image: routing hands the limit to
/// IHttpMaxRequestBodySizeFeature, which only a real server implements. TestServer has no such feature, so
/// the host these tests run on would accept the body whatever the limit says.
/// </summary>
[Trait("Category", "Integration")]
public class UploadRequestSizeLimitTests(ImagerApiFixture fixture) : ImagerApiTests(fixture)
{
    private const string UploadRoute = "/images/upload";
    private const string UploadByUrlRoute = "/images/upload-by-url";

    [Fact]
    public void Upload_BoundsTheRequestBodyByTheConfiguredMaxImageSize()
    {
        var maxImageBytes = Fixture.Services
            .GetRequiredService<IOptions<ImageLimitationsSettings>>().Value.SizeBytes.Max;

        var limit = RequestSizeLimitOf(UploadRoute);

        Assert.NotNull(limit);
        Assert.True(limit > maxImageBytes,
            $"A limit of {limit} leaves no room for the multipart framing around a {maxImageBytes} byte image.");
        Assert.True(limit <= maxImageBytes + 64 * 1024,
            $"A limit of {limit} is far above the {maxImageBytes} byte image it is meant to bound.");
    }

    [Fact]
    public void UploadByUrl_BoundsTheRequestBodyToASmallJsonDocument()
    {
        var limit = RequestSizeLimitOf(UploadByUrlRoute);

        Assert.NotNull(limit);
        //the body is a url and a handful of short fields - the image itself never travels through it, and
        //a limit anywhere near the multipart one would mean that number had been copied rather than chosen
        Assert.InRange(limit.Value, 1024, 64 * 1024);
    }

    private long? RequestSizeLimitOf(string route) =>
        Fixture.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.RoutePattern.RawText == route)
            .Metadata.GetMetadata<IRequestSizeLimitMetadata>()?.MaxRequestBodySize;
}
