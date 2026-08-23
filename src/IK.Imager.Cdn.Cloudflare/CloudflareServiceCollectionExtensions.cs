using System;
using System.Net.Http.Headers;
using IK.Imager.Core.Abstractions.Cdn;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace IK.Imager.Cdn.Cloudflare;

public static class CloudflareServiceCollectionExtensions
{
    /// <summary>
    /// Configuration section this module binds <see cref="CloudflareCdnSettings"/> from.
    /// </summary>
    public const string SectionName = "Cdn:Cloudflare";

    private static readonly Uri ApiBaseAddress = new("https://api.cloudflare.com/");

    /// <summary>
    /// Registers the Cloudflare implementation of <see cref="ICdnPurger"/>
    /// and binds its settings from the <see cref="SectionName"/> section.
    /// </summary>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <param name="configuration">The configuration root - the module locates its own section within it.</param>
    public static IServiceCollection AddCloudflareCdnPurger(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        section.Require("ZoneId");
        section.Require("ApiToken");

        services.Configure<CloudflareCdnSettings>(section);

        //Core registers NoOpCdnPurger with TryAdd and runs first, so TryAdd here would lose silently
        services.RemoveAll<ICdnPurger>();

        services.AddHttpClient<ICdnPurger, CloudflareCdnPurger>((provider, client) =>
        {
            client.BaseAddress = ApiBaseAddress;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", provider.GetRequiredService<IOptions<CloudflareCdnSettings>>().Value.ApiToken);
        });

        return services;
    }

    private static void Require(this IConfigurationSection section, string key)
    {
        if (string.IsNullOrWhiteSpace(section[key]))
            throw new InvalidOperationException($"'{section.Path}:{key}' is required to purge the CDN.");
    }
}
