using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Cdn.Akamai;
using IK.Imager.Cdn.Tests.Infrastructure;
using IK.Imager.Core.Abstractions.Cdn;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IK.Imager.Cdn.Tests.Akamai;

public class AkamaiCdnPurgerTests
{
    private const string Host = "akab-testhost.purge.akamaiapis.net";
    private const string Accepted = """{"purgeId":"purge-id","estimatedSeconds":5,"httpStatus":201}""";
    private const int MaxBodyBytes = 50 * 1024;

    private static readonly Uri ImageUri = new("https://cdn.test/images/abc.jpg");

    private readonly RecordingHttpMessageHandler _handler = new();

    private ICdnPurger CreatePurger() =>
        new AkamaiCdnPurger(_handler.CreateClient($"https://{Host}/"), NullLogger<AkamaiCdnPurger>.Instance);

    private static string[] PurgedObjects(RecordingHttpMessageHandler.RecordedRequest request) =>
        JsonDocument.Parse(request.Body!).RootElement.GetProperty("objects")
            .EnumerateArray().Select(x => x.GetString()!).ToArray();

    //long enough that the 50 KB body limit is reached within a readable number of uris
    private static IReadOnlyCollection<Uri> LongImageUris(int count) =>
        Enumerable.Range(0, count)
            .Select(x => new Uri($"https://cdn.test/images/{new string('a', 480)}{x}.jpg"))
            .ToList();

    [Fact]
    public async Task Purge_SingleUri_PostsTheUrlToTheProductionDeleteEndpoint()
    {
        _handler.Responds(HttpStatusCode.Created, Accepted);

        await CreatePurger().Purge([ImageUri], CancellationToken.None);

        var request = Assert.Single(_handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal($"https://{Host}/ccu/v3/delete/url/production", request.RequestUri.AbsoluteUri);
        Assert.Equal([ImageUri.AbsoluteUri], PurgedObjects(request));
    }

    [Fact]
    public async Task Purge_NoUris_SendsNothing()
    {
        await CreatePurger().Purge([], CancellationToken.None);

        Assert.Empty(_handler.Requests);
    }

    /// <summary>
    /// Fast Purge caps the request body rather than the number of objects, so the uris are split by the
    /// size they serialize to.
    /// </summary>
    [Fact]
    public async Task Purge_UrisExceedingTheBodyLimit_SplitsThemAcrossRequests()
    {
        var uris = LongImageUris(300);
        _handler.Responds(10, HttpStatusCode.Created, Accepted);

        await CreatePurger().Purge(uris, CancellationToken.None);

        Assert.True(_handler.Requests.Count > 1, "the uris should not have fitted into one request");
        Assert.All(_handler.Requests,
            x => Assert.True(Encoding.UTF8.GetByteCount(x.Body!) <= MaxBodyBytes,
                $"a request body of {Encoding.UTF8.GetByteCount(x.Body!)} bytes passes the {MaxBodyBytes} byte limit"));
        Assert.Equal(uris.Select(x => x.AbsoluteUri), _handler.Requests.SelectMany(PurgedObjects));
    }

    [Fact]
    public async Task Purge_UrisFittingInOneBody_SendsASingleRequest()
    {
        _handler.Responds(HttpStatusCode.Created, Accepted);

        await CreatePurger().Purge(LongImageUris(50), CancellationToken.None);

        Assert.Single(_handler.Requests);
    }

    [Fact]
    public async Task Purge_ErrorStatusCode_Throws()
    {
        _handler.Responds(HttpStatusCode.Forbidden, "forbidden");

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            CreatePurger().Purge([ImageUri], CancellationToken.None));

        Assert.Contains("403", exception.Message);
    }

    /// <summary>
    /// The signature itself is covered by <see cref="EdgeGridSignerTests"/>. What matters here is that the
    /// signing handler is in the pipeline at all - without it every purge comes back unauthorized.
    /// </summary>
    [Fact]
    public async Task AddAkamaiCdnPurger_Configured_SignsTheRequestItSends()
    {
        var services = new ServiceCollection().AddAkamaiCdnPurger(ModuleRegistration.Configuration(
            ($"{AkamaiServiceCollectionExtensions.SectionName}:Host", Host),
            ($"{AkamaiServiceCollectionExtensions.SectionName}:ClientToken", "client-token"),
            ($"{AkamaiServiceCollectionExtensions.SectionName}:ClientSecret", "client-secret"),
            ($"{AkamaiServiceCollectionExtensions.SectionName}:AccessToken", "access-token")));

        _handler.Responds(HttpStatusCode.Created, Accepted);
        services.AddHttpClient(ModuleRegistration.ClientName)
            .ConfigurePrimaryHttpMessageHandler(() => _handler);

        using var provider = ModuleRegistration.BuildProvider(services);

        var purger = provider.GetRequiredService<ICdnPurger>();
        Assert.IsType<AkamaiCdnPurger>(purger);

        await purger.Purge([ImageUri], CancellationToken.None);

        var authorization = Assert.Single(_handler.Requests).Headers["Authorization"];
        Assert.StartsWith("EG1-HMAC-SHA256 client_token=client-token;", authorization);
        Assert.Contains(";signature=", authorization);
    }

    [Fact]
    public void AddAkamaiCdnPurger_MissingClientSecret_Throws()
    {
        var configuration = ModuleRegistration.Configuration(
            ($"{AkamaiServiceCollectionExtensions.SectionName}:Host", Host),
            ($"{AkamaiServiceCollectionExtensions.SectionName}:ClientToken", "client-token"),
            ($"{AkamaiServiceCollectionExtensions.SectionName}:AccessToken", "access-token"));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddAkamaiCdnPurger(configuration));

        Assert.Contains("ClientSecret", exception.Message);
    }
}
