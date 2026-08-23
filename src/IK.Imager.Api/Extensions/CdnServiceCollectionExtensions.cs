using System;
using IK.Imager.Cdn.Akamai;
using IK.Imager.Cdn.AzureFrontDoor;
using IK.Imager.Cdn.Cloudflare;
using IK.Imager.Cdn.Fastly;
using IK.Imager.Core.Abstractions.Cdn;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable 1591

namespace IK.Imager.Api.Extensions;

public static class CdnServiceCollectionExtensions
{
    /// <summary>
    /// Configuration value naming the CDN this deployment sits behind.
    /// </summary>
    public const string ProviderPath = "Cdn:Provider";

    /// <summary>
    /// Registers the purger of the configured CDN provider. Leaving the provider unset keeps the no-op
    /// purger the core registers, which is what a deployment without a CDN wants.
    /// </summary>
    /// <remarks>
    /// The selection lives in the host rather than in a module for the same reason the health checks do:
    /// which CDN a deployment uses is an operational decision, and every module already owns its own
    /// registration. An unknown provider throws instead of falling back - a typo in Cdn__Provider would
    /// otherwise look like a service that purges.
    /// </remarks>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <param name="configuration">The configuration root - each module locates its own section within it.</param>
    public static IServiceCollection AddCdnPurger(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>(ProviderPath);
        if (string.IsNullOrWhiteSpace(provider))
            return services;

        return provider.ToLowerInvariant() switch
        {
            "cloudflare" => services.AddCloudflareCdnPurger(configuration),
            "azurefrontdoor" => services.AddAzureFrontDoorCdnPurger(configuration),
            "fastly" => services.AddFastlyCdnPurger(configuration),
            "akamai" => services.AddAkamaiCdnPurger(configuration),
            _ => throw new InvalidOperationException(
                $"'{ProviderPath}' is set to '{provider}', which is not a CDN this service can purge. " +
                $"Use Cloudflare, AzureFrontDoor, Fastly or Akamai, or leave it empty to disable purging.")
        };
    }
}
