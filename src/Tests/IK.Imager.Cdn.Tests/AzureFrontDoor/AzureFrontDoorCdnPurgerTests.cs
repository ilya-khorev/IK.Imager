using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.ResourceManager;
using IK.Imager.Cdn.AzureFrontDoor;
using IK.Imager.Cdn.Tests.Infrastructure;
using IK.Imager.Core.Abstractions.Cdn;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace IK.Imager.Cdn.Tests.AzureFrontDoor;

public class AzureFrontDoorCdnPurgerTests
{
    private const string SubscriptionId = "00000000-0000-0000-0000-000000000000";
    private const string ResourceGroupName = "images-rg";
    private const string ProfileName = "images-profile";
    private const string EndpointName = "images-endpoint";

    private static readonly Uri ImageUri = new("https://cdn.test/images/abc.jpg");
    private static readonly Uri ThumbnailUri = new("https://cdn.test/thumbnails/abc.jpg");

    private readonly RecordingHttpMessageHandler _handler = new();

    private ICdnPurger CreatePurger()
    {
        var options = new ArmClientOptions
        {
            Transport = new HttpClientTransport(new System.Net.Http.HttpClient(_handler))
        };
        //a stubbed failure should surface at once rather than after the pipeline has retried it
        options.Retry.MaxRetries = 0;

        var armClient = new ArmClient(new StubTokenCredential(), SubscriptionId, options);

        return new AzureFrontDoorCdnPurger(armClient, Options.Create(new AzureFrontDoorCdnSettings
        {
            SubscriptionId = SubscriptionId,
            ResourceGroupName = ResourceGroupName,
            ProfileName = ProfileName,
            EndpointName = EndpointName
        }), NullLogger<AzureFrontDoorCdnPurger>.Instance);
    }

    private static string[] Values(RecordingHttpMessageHandler.RecordedRequest request, string property) =>
        JsonDocument.Parse(request.Body!).RootElement.GetProperty(property)
            .EnumerateArray().Select(x => x.GetString()!).ToArray();

    [Fact]
    public async Task Purge_SingleUri_PostsToTheEndpointsPurgeOperation()
    {
        _handler.Responds(HttpStatusCode.OK, "{}");

        await CreatePurger().Purge([ImageUri], CancellationToken.None);

        var request = Assert.Single(_handler.Requests);
        Assert.Equal(
            $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}" +
            $"/providers/Microsoft.Cdn/profiles/{ProfileName}/afdEndpoints/{EndpointName}/purge",
            request.RequestUri.AbsolutePath);
    }

    /// <summary>
    /// Front Door takes paths, not urls.
    /// </summary>
    [Fact]
    public async Task Purge_SeveralUris_SendsTheirPathsInOneRequest()
    {
        _handler.Responds(HttpStatusCode.OK, "{}");

        await CreatePurger().Purge([ImageUri, ThumbnailUri], CancellationToken.None);

        Assert.Equal(["/images/abc.jpg", "/thumbnails/abc.jpg"],
            Values(Assert.Single(_handler.Requests), "contentPaths"));
    }

    /// <summary>
    /// What Front Door purges when the domains are left out is undocumented, so they are always sent -
    /// taken from the uris, which are already on the CDN host.
    /// </summary>
    [Fact]
    public async Task Purge_UrisOnTheSameHost_SendsThatHostOnceAsADomain()
    {
        _handler.Responds(HttpStatusCode.OK, "{}");

        await CreatePurger().Purge([ImageUri, ThumbnailUri], CancellationToken.None);

        Assert.Equal(["cdn.test"], Values(Assert.Single(_handler.Requests), "domains"));
    }

    [Fact]
    public async Task Purge_NoUris_SendsNothing()
    {
        await CreatePurger().Purge([], CancellationToken.None);

        Assert.Empty(_handler.Requests);
    }

    /// <summary>
    /// Front Door refuses a second purge until the first one has finished, which takes about ten minutes,
    /// so splitting a larger batch would fail on its second request. Refusing it up front says so.
    /// </summary>
    [Fact]
    public async Task Purge_MoreUrisThanOneRequestAllows_ThrowsWithoutSendingAnything()
    {
        var uris = Enumerable.Range(0, 101)
            .Select(x => new Uri($"https://cdn.test/images/{x}.jpg")).ToList();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreatePurger().Purge(uris, CancellationToken.None));

        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task Purge_ExactlyTheMaximumNumberOfUris_IsSent()
    {
        var uris = Enumerable.Range(0, 100)
            .Select(x => new Uri($"https://cdn.test/images/{x}.jpg")).ToList();
        _handler.Responds(HttpStatusCode.OK, "{}");

        await CreatePurger().Purge(uris, CancellationToken.None);

        Assert.Equal(100, Values(Assert.Single(_handler.Requests), "contentPaths").Length);
    }

    [Fact]
    public async Task Purge_ErrorStatusCode_Throws()
    {
        _handler.Responds(HttpStatusCode.Forbidden, "{}");

        await Assert.ThrowsAsync<global::Azure.RequestFailedException>(() =>
            CreatePurger().Purge([ImageUri], CancellationToken.None));
    }

    [Fact]
    public void AddAzureFrontDoorCdnPurger_Configured_RegistersThePurger()
    {
        var section = AzureFrontDoorServiceCollectionExtensions.SectionName;
        var services = new ServiceCollection().AddAzureFrontDoorCdnPurger(ModuleRegistration.Configuration(
            ($"{section}:SubscriptionId", SubscriptionId),
            ($"{section}:ResourceGroupName", ResourceGroupName),
            ($"{section}:ProfileName", ProfileName),
            ($"{section}:EndpointName", EndpointName)));

        using var provider = ModuleRegistration.BuildProvider(services);

        Assert.IsType<AzureFrontDoorCdnPurger>(provider.GetRequiredService<ICdnPurger>());
    }

    [Fact]
    public void AddAzureFrontDoorCdnPurger_MissingEndpointName_Throws()
    {
        var section = AzureFrontDoorServiceCollectionExtensions.SectionName;
        var configuration = ModuleRegistration.Configuration(
            ($"{section}:SubscriptionId", SubscriptionId),
            ($"{section}:ResourceGroupName", ResourceGroupName),
            ($"{section}:ProfileName", ProfileName));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddAzureFrontDoorCdnPurger(configuration));

        Assert.Contains("EndpointName", exception.Message);
    }

    /// <summary>
    /// Keeps the management API out of the test - the pipeline never leaves the process, so it only has
    /// to hand out a token that looks valid.
    /// </summary>
    private sealed class StubTokenCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new("stub-token", DateTimeOffset.UtcNow.AddHours(1));

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext,
            CancellationToken cancellationToken) =>
            new(GetToken(requestContext, cancellationToken));
    }
}
