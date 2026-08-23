using System;
using IK.Imager.Core.Abstractions.Cdn;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace IK.Imager.Cdn.Akamai;

public static class AkamaiServiceCollectionExtensions
{
    /// <summary>
    /// Configuration section this module binds <see cref="AkamaiCdnSettings"/> from.
    /// </summary>
    public const string SectionName = "Cdn:Akamai";

    /// <summary>
    /// Registers the Akamai implementation of <see cref="ICdnPurger"/>
    /// and binds its settings from the <see cref="SectionName"/> section.
    /// </summary>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <param name="configuration">The configuration root - the module locates its own section within it.</param>
    public static IServiceCollection AddAkamaiCdnPurger(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        section.Require("Host");
        section.Require("ClientToken");
        section.Require("ClientSecret");
        section.Require("AccessToken");

        services.Configure<AkamaiCdnSettings>(section);
        services.AddTransient<EdgeGridAuthenticationHandler>();

        //Core registers NoOpCdnPurger with TryAdd and runs first, so TryAdd here would lose silently
        services.RemoveAll<ICdnPurger>();

        services.AddHttpClient<ICdnPurger, AkamaiCdnPurger>((provider, client) =>
            {
                var host = provider.GetRequiredService<IOptions<AkamaiCdnSettings>>().Value.Host;
                client.BaseAddress = new Uri($"https://{host}/");
            })
            .AddHttpMessageHandler<EdgeGridAuthenticationHandler>();

        return services;
    }

    private static void Require(this IConfigurationSection section, string key)
    {
        if (string.IsNullOrWhiteSpace(section[key]))
            throw new InvalidOperationException($"'{section.Path}:{key}' is required to purge the CDN.");
    }
}
