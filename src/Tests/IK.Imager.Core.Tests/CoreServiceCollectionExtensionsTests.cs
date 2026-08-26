using System;
using System.Collections.Generic;
using System.Net.Http;
using IK.Imager.Core.Abstractions.Upload;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IK.Imager.Core.Tests;

/// <summary>
/// The image download client is the one place the service fetches a url a caller chose, so its time bound
/// and its primary handler are part of the registration rather than something a host has to remember.
/// </summary>
public class CoreServiceCollectionExtensionsTests
{
    private const string ClientName = nameof(IImageDownloader);

    [Fact]
    public void AddImagerCore_WithoutConfiguration_BoundsTheDownloadByTheDefaultTimeout()
    {
        using var client = CreateDownloadClient();

        Assert.Equal(TimeSpan.FromSeconds(30), client.Timeout);
    }

    [Fact]
    public void AddImagerCore_TimeoutInConfiguration_IsApplied()
    {
        using var client = CreateDownloadClient(new Dictionary<string, string?>
        {
            ["ImageDownload:Timeout"] = "00:00:05"
        });

        Assert.Equal(TimeSpan.FromSeconds(5), client.Timeout);
    }

    /// <summary>
    /// The address checks live in the primary handler, so a registration that lost it would keep working and
    /// quietly download from anywhere - the same trap ICdnPurger has with TryAdd.
    /// </summary>
    [Fact]
    public void AddImagerCore_DownloadClient_ChecksTheAddressItConnectsTo()
    {
        var provider = BuildProvider([]);

        var handler = provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(ClientName);
        while (handler is DelegatingHandler delegatingHandler && delegatingHandler.InnerHandler != null)
            handler = delegatingHandler.InnerHandler;

        var socketsHandler = Assert.IsType<SocketsHttpHandler>(handler);
        Assert.NotNull(socketsHandler.ConnectCallback);
        Assert.False(socketsHandler.AllowAutoRedirect);
    }

    private static HttpClient CreateDownloadClient(Dictionary<string, string?>? settings = null) =>
        BuildProvider(settings ?? []).GetRequiredService<IHttpClientFactory>().CreateClient(ClientName);

    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new ServiceCollection().AddImagerCore(configuration).BuildServiceProvider();
    }
}
