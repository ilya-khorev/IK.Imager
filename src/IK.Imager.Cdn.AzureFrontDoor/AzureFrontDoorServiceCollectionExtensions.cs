using System;
using Azure.Identity;
using Azure.ResourceManager;
using IK.Imager.Core.Abstractions.Cdn;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IK.Imager.Cdn.AzureFrontDoor;

public static class AzureFrontDoorServiceCollectionExtensions
{
    /// <summary>
    /// Configuration section this module binds <see cref="AzureFrontDoorCdnSettings"/> from.
    /// </summary>
    public const string SectionName = "Cdn:AzureFrontDoor";

    /// <summary>
    /// Registers the Azure Front Door implementation of <see cref="ICdnPurger"/>
    /// and binds its settings from the <see cref="SectionName"/> section.
    /// </summary>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <param name="configuration">The configuration root - the module locates its own section within it.</param>
    public static IServiceCollection AddAzureFrontDoorCdnPurger(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        section.Require("SubscriptionId");
        section.Require("ResourceGroupName");
        section.Require("ProfileName");
        section.Require("EndpointName");

        services.Configure<AzureFrontDoorCdnSettings>(section);

        //TryAdd so that a host with its own credential setup can register the client itself
        services.TryAddSingleton(_ => new ArmClient(new DefaultAzureCredential()));

        //Core registers NoOpCdnPurger with TryAdd and runs first, so TryAdd here would lose silently
        services.RemoveAll<ICdnPurger>();
        services.AddSingleton<ICdnPurger, AzureFrontDoorCdnPurger>();

        return services;
    }

    private static void Require(this IConfigurationSection section, string key)
    {
        if (string.IsNullOrWhiteSpace(section[key]))
            throw new InvalidOperationException($"'{section.Path}:{key}' is required to purge the CDN.");
    }
}
