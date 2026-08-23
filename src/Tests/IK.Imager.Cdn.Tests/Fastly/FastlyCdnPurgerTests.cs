using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Cdn.Fastly;
using IK.Imager.Cdn.Tests.Infrastructure;
using IK.Imager.Core.Abstractions.Cdn;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IK.Imager.Cdn.Tests.Fastly;

public class FastlyCdnPurgerTests
{
    private const string ApiToken = "api-token";
    private const string Ok = """{"status":"ok","id":"purge-id"}""";

    private static readonly Uri ImageUri = new("https://cdn.test/images/abc.jpg");
    private static readonly Uri ThumbnailUri = new("https://cdn.test/thumbnails/abc.jpg");

    private readonly RecordingHttpMessageHandler _handler = new();

    private ICdnPurger CreatePurger() =>
        new FastlyCdnPurger(_handler.CreateClient("https://api.fastly.com/"),
            NullLogger<FastlyCdnPurger>.Instance);

    [Fact]
    public async Task Purge_SingleUri_PostsTheHostAndPathWithTheSchemeStripped()
    {
        _handler.Responds(HttpStatusCode.OK, Ok);

        await CreatePurger().Purge([ImageUri], CancellationToken.None);

        var request = Assert.Single(_handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.fastly.com/purge/cdn.test/images/abc.jpg", request.RequestUri.AbsoluteUri);
    }

    /// <summary>
    /// Fastly has no bulk purge by url, so a set of uris is a request each.
    /// </summary>
    [Fact]
    public async Task Purge_SeveralUris_SendsOneRequestPerUri()
    {
        _handler.Responds(2, HttpStatusCode.OK, Ok);

        await CreatePurger().Purge([ImageUri, ThumbnailUri], CancellationToken.None);

        Assert.Equal(
            ["https://api.fastly.com/purge/cdn.test/images/abc.jpg",
             "https://api.fastly.com/purge/cdn.test/thumbnails/abc.jpg"],
            _handler.Requests.Select(x => x.RequestUri.AbsoluteUri).ToArray());
    }

    /// <summary>
    /// A soft purge would mark the object stale instead of removing it, and the blob it would revalidate
    /// against is already gone.
    /// </summary>
    [Fact]
    public async Task Purge_AnyUri_DoesNotAskForASoftPurge()
    {
        _handler.Responds(HttpStatusCode.OK, Ok);

        await CreatePurger().Purge([ImageUri], CancellationToken.None);

        Assert.DoesNotContain("Fastly-Soft-Purge", Assert.Single(_handler.Requests).Headers.Keys);
    }

    [Fact]
    public async Task Purge_NoUris_SendsNothing()
    {
        await CreatePurger().Purge([], CancellationToken.None);

        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task Purge_ErrorStatusCode_Throws()
    {
        _handler.Responds(HttpStatusCode.TooManyRequests, "rate limited");

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            CreatePurger().Purge([ImageUri], CancellationToken.None));

        Assert.Contains("429", exception.Message);
    }

    /// <summary>
    /// Stops at the failure rather than carrying on. The consumer retries the whole set, and purging the
    /// same uri twice costs nothing.
    /// </summary>
    [Fact]
    public async Task Purge_SecondUriFails_ThrowsWithoutSendingTheRest()
    {
        _handler.Responds(HttpStatusCode.OK, Ok).Responds(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            CreatePurger().Purge([ImageUri, ThumbnailUri, new Uri("https://cdn.test/images/z.jpg")],
                CancellationToken.None));

        Assert.Equal(2, _handler.Requests.Count);
    }

    [Fact]
    public void AddFastlyCdnPurger_Configured_RegistersTheClientWithTheApiKeyHeader()
    {
        var services = new ServiceCollection().AddFastlyCdnPurger(ModuleRegistration.Configuration(
            ($"{FastlyServiceCollectionExtensions.SectionName}:ApiToken", ApiToken)));

        using var provider = ModuleRegistration.BuildProvider(services);

        Assert.IsType<FastlyCdnPurger>(provider.GetRequiredService<ICdnPurger>());

        var client = ModuleRegistration.CdnHttpClient(provider);
        Assert.Equal("https://api.fastly.com/", client.BaseAddress!.AbsoluteUri);
        Assert.Equal(ApiToken, Assert.Single(client.DefaultRequestHeaders.GetValues("Fastly-Key")));
    }

    [Fact]
    public void AddFastlyCdnPurger_MissingApiToken_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddFastlyCdnPurger(ModuleRegistration.Configuration()));

        Assert.Contains("ApiToken", exception.Message);
    }
}
