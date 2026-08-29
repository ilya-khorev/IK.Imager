using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable 1591

namespace IK.Imager.Api.Tenancy;

public static class TenancyServiceCollectionExtensions
{
    public const string SectionName = "Tenancy";

    /// <summary>
    /// Registers the tenant resolver this deployment uses, and the scoped context the endpoints read.
    /// </summary>
    /// <remarks>
    /// The host picks the source for the same reason it picks the CDN provider: where identity comes from is
    /// an operational decision. An unrecognised source throws instead of falling back, because a typo in
    /// Tenancy__Source would otherwise look like a service that is still isolating tenants.
    /// </remarks>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <param name="configuration">The configuration root - this module locates its own section within it.</param>
    public static IServiceCollection AddTenancy(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        services.Configure<TenancySettings>(section);

        var settings = section.Get<TenancySettings>() ?? new TenancySettings();
        var source = string.IsNullOrWhiteSpace(settings.Source) ? TenantSources.Header : settings.Source;

        switch (source.ToLowerInvariant())
        {
            case "header":
                services.AddSingleton<ITenantResolver, HeaderTenantResolver>();
                break;

            case "claim":
                if (string.IsNullOrWhiteSpace(settings.ClaimType))
                    throw new InvalidOperationException(
                        $"'{SectionName}:{nameof(TenancySettings.ClaimType)}' must name the claim carrying the tenant " +
                        $"when '{SectionName}:{nameof(TenancySettings.Source)}' is '{TenantSources.Claim}'.");

                services.AddSingleton<ITenantResolver, ClaimsTenantResolver>();
                break;

            default:
                throw new InvalidOperationException(
                    $"'{SectionName}:{nameof(TenancySettings.Source)}' is set to '{settings.Source}', which is not a " +
                    $"tenant source this service knows. Use '{TenantSources.Header}' or '{TenantSources.Claim}'.");
        }

        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(provider => provider.GetRequiredService<TenantContext>());

        return services;
    }
}
