using System;
using IK.Imager.Core.Abstractions.Cdn;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace IK.Imager.Cdn.Fastly;

public static class FastlyServiceCollectionExtensions
{
    /// <summary>
    /// Configuration section this module binds <see cref="FastlyCdnSettings"/> from.
    /// </summary>
    public const string SectionName = "Cdn:Fastly";

    private const string ApiKeyHeader = "Fastly-Key";

    private static readonly Uri ApiBaseAddress = new("https://api.fastly.com/");

    /// <summary>
    /// Registers the Fastly implementation of <see cref="ICdnPurger"/>
    /// and binds its settings from the <see cref="SectionName"/> section.
    /// </summary>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <param name="configuration">The configuration root - the module locates its own section within it.</param>
    public static IServiceCollection AddFastlyCdnPurger(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        if (string.IsNullOrWhiteSpace(section["ApiToken"]))
            throw new InvalidOperationException($"'{section.Path}:ApiToken' is required to purge the CDN.");

        services.Configure<FastlyCdnSettings>(section);

        //Core registers NoOpCdnPurger with TryAdd and runs first, so TryAdd here would lose silently
        services.RemoveAll<ICdnPurger>();

        services.AddHttpClient<ICdnPurger, FastlyCdnPurger>((provider, client) =>
        {
            client.BaseAddress = ApiBaseAddress;
            //url purges are unauthenticated by default, but a service may require auth for them
            client.DefaultRequestHeaders.Add(ApiKeyHeader,
                provider.GetRequiredService<IOptions<FastlyCdnSettings>>().Value.ApiToken);
        });

        return services;
    }
}
