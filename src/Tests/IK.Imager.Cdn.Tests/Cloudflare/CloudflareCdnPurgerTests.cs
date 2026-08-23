using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Cdn.Cloudflare;
using IK.Imager.Cdn.Tests.Infrastructure;
using IK.Imager.Core.Abstractions.Cdn;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace IK.Imager.Cdn.Tests.Cloudflare;

public class CloudflareCdnPurgerTests
{
    private const string ZoneId = "zone-id";
    private const string ApiToken = "api-token";
    private const string Ok = """{"success":true,"errors":[],"result":{"id":"purge-id"}}""";

    private static readonly Uri ImageUri = new("https://cdn.test/images/abc.jpg");

    private readonly RecordingHttpMessageHandler _handler = new();

    private ICdnPurger CreatePurger() =>
        new CloudflareCdnPurger(_handler.CreateClient("https://api.cloudflare.com/"),
            Options.Create(new CloudflareCdnSettings { ZoneId = ZoneId, ApiToken = ApiToken }),
            NullLogger<CloudflareCdnPurger>.Instance);

    private static IReadOnlyCollection<Uri> ImageUris(int count) =>
        Enumerable.Range(0, count).Select(x => new Uri($"https://cdn.test/images/{x}.jpg")).ToList();

    private static string[] PurgedFiles(RecordingHttpMessageHandler.RecordedRequest request) =>
        JsonDocument.Parse(request.Body!).RootElement.GetProperty("files")
            .EnumerateArray().Select(x => x.GetString()!).ToArray();

    [Fact]
    public async Task Purge_SingleUri_PostsTheUrlToTheZonePurgeEndpoint()
    {
        _handler.Responds(HttpStatusCode.OK, Ok);

        await CreatePurger().Purge([ImageUri], CancellationToken.None);

        var request = Assert.Single(_handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal($"https://api.cloudflare.com/client/v4/zones/{ZoneId}/purge_cache",
            request.RequestUri.AbsoluteUri);
        Assert.Equal([ImageUri.AbsoluteUri], PurgedFiles(request));
    }

    [Fact]
    public async Task Purge_NoUris_SendsNothing()
    {
        await CreatePurger().Purge([], CancellationToken.None);

        Assert.Empty(_handler.Requests);
    }

    /// <summary>
    /// Cloudflare rejects a purge-by-url call carrying more than 100 urls, so a larger set has to arrive
    /// as several requests - with every uri purged exactly once.
    /// </summary>
    [Fact]
    public async Task Purge_MoreUrisThanOneRequestAllows_SplitsThemAcrossRequests()
    {
        var uris = ImageUris(250);
        _handler.Responds(3, HttpStatusCode.OK, Ok);

        await CreatePurger().Purge(uris, CancellationToken.None);

        Assert.Equal(3, _handler.Requests.Count);
        Assert.Equal([100, 100, 50], _handler.Requests.Select(x => PurgedFiles(x).Length).ToArray());
        Assert.Equal(uris.Select(x => x.AbsoluteUri),
            _handler.Requests.SelectMany(PurgedFiles));
    }

    [Fact]
    public async Task Purge_ErrorStatusCode_Throws()
    {
        _handler.Responds(HttpStatusCode.Unauthorized, """{"success":false,"errors":[{"code":10000}]}""");

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            CreatePurger().Purge([ImageUri], CancellationToken.None));

        Assert.Contains("401", exception.Message);
    }

    /// <summary>
    /// Cloudflare does not promise that the status code and the envelope agree. A 200 whose body says the
    /// purge failed has to fail here, or a deleted image quietly stays on the edge.
    /// </summary>
    [Fact]
    public async Task Purge_SuccessStatusCodeCarryingAFailedEnvelope_Throws()
    {
        _handler.Responds(HttpStatusCode.OK,
            """{"success":false,"errors":[{"code":1012,"message":"Invalid request"}]}""");

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            CreatePurger().Purge([ImageUri], CancellationToken.None));

        Assert.Contains("1012", exception.Message);
        Assert.Contains("Invalid request", exception.Message);
    }

    [Fact]
    public async Task Purge_CancelledToken_DoesNotComplete()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreatePurger().Purge([ImageUri], cancellation.Token));
    }

    [Fact]
    public void AddCloudflareCdnPurger_Configured_RegistersTheClientWithBearerAuthentication()
    {
        var services = new ServiceCollection().AddCloudflareCdnPurger(ModuleRegistration.Configuration(
            ($"{CloudflareServiceCollectionExtensions.SectionName}:ZoneId", ZoneId),
            ($"{CloudflareServiceCollectionExtensions.SectionName}:ApiToken", ApiToken)));

        using var provider = ModuleRegistration.BuildProvider(services);

        Assert.IsType<CloudflareCdnPurger>(provider.GetRequiredService<ICdnPurger>());

        var client = ModuleRegistration.CdnHttpClient(provider);
        Assert.Equal("https://api.cloudflare.com/", client.BaseAddress!.AbsoluteUri);
        Assert.Equal("Bearer", client.DefaultRequestHeaders.Authorization!.Scheme);
        Assert.Equal(ApiToken, client.DefaultRequestHeaders.Authorization.Parameter);
    }

    [Fact]
    public void AddCloudflareCdnPurger_MissingApiToken_Throws()
    {
        var configuration = ModuleRegistration.Configuration(
            ($"{CloudflareServiceCollectionExtensions.SectionName}:ZoneId", ZoneId));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddCloudflareCdnPurger(configuration));

        Assert.Contains("ApiToken", exception.Message);
    }
}
