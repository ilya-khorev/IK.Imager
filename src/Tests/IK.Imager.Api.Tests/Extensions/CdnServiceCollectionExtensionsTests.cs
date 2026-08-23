using System;
using System.Collections.Generic;
using IK.Imager.Api.Extensions;
using IK.Imager.Cdn.Akamai;
using IK.Imager.Cdn.AzureFrontDoor;
using IK.Imager.Cdn.Cloudflare;
using IK.Imager.Cdn.Fastly;
using IK.Imager.Core;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Cdn;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IK.Imager.Api.Tests.Extensions;

/// <summary>
/// The Cdn:Provider switch, over the real composition order of Program.cs.
///
/// AddImagerCore runs first and registers NoOpCdnPurger with TryAdd, so a provider module that also used
/// TryAdd would lose and the service would silently never purge. These tests are what pins that down.
/// </summary>
public class CdnServiceCollectionExtensionsTests
{
    private static readonly (string Key, string Value)[] AllProviderSettings =
    [
        ("Cdn:Cloudflare:ZoneId", "zone-id"),
        ("Cdn:Cloudflare:ApiToken", "api-token"),
        ("Cdn:AzureFrontDoor:SubscriptionId", "00000000-0000-0000-0000-000000000000"),
        ("Cdn:AzureFrontDoor:ResourceGroupName", "images-rg"),
        ("Cdn:AzureFrontDoor:ProfileName", "images-profile"),
        ("Cdn:AzureFrontDoor:EndpointName", "images-endpoint"),
        ("Cdn:Fastly:ApiToken", "api-token"),
        ("Cdn:Akamai:Host", "akab-testhost.purge.akamaiapis.net"),
        ("Cdn:Akamai:ClientToken", "client-token"),
        ("Cdn:Akamai:ClientSecret", "client-secret"),
        ("Cdn:Akamai:AccessToken", "access-token")
    ];

    private static IConfiguration Configuration(string? provider)
    {
        var values = new Dictionary<string, string?> { ["Cdn:Provider"] = provider };
        foreach (var (key, value) in AllProviderSettings)
            values[key] = value;

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    //the order Program.cs registers them in, which is what makes the TryAdd question real
    private static ICdnPurger ResolvePurger(string? provider)
    {
        var configuration = Configuration(provider);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddImagerCore(configuration);
        services.AddCdnPurger(configuration);

        return services.BuildServiceProvider().GetRequiredService<ICdnPurger>();
    }

    [Theory]
    [InlineData("Cloudflare", typeof(CloudflareCdnPurger))]
    [InlineData("AzureFrontDoor", typeof(AzureFrontDoorCdnPurger))]
    [InlineData("Fastly", typeof(FastlyCdnPurger))]
    [InlineData("Akamai", typeof(AkamaiCdnPurger))]
    public void AddCdnPurger_ConfiguredProvider_ReplacesTheNoOpPurger(string provider, Type expected)
    {
        Assert.IsType(expected, ResolvePurger(provider));
    }

    /// <summary>
    /// Configuration keys are matched case insensitively everywhere else, so the provider name is too.
    /// </summary>
    [Theory]
    [InlineData("cloudflare")]
    [InlineData("CLOUDFLARE")]
    public void AddCdnPurger_ProviderInAnyCase_IsRecognised(string provider)
    {
        Assert.IsType<CloudflareCdnPurger>(ResolvePurger(provider));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddCdnPurger_NoProvider_KeepsTheNoOpPurger(string? provider)
    {
        Assert.IsType<NoOpCdnPurger>(ResolvePurger(provider));
    }

    /// <summary>
    /// A typo in Cdn__Provider must not read as a working deployment that quietly never purges.
    /// </summary>
    [Fact]
    public void AddCdnPurger_UnknownProvider_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => ResolvePurger("CloudFront"));

        Assert.Contains("CloudFront", exception.Message);
    }
}
