using System;
using FluentValidation;
using IK.Imager.Api.ExceptionHandling;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable 1591

namespace IK.Imager.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the minimal API endpoints need - the exception handler that turns an unhandled
    /// exception into a response, and the request validators the endpoint filters resolve.
    /// </summary>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <param name="configureAuthentication">
    /// Optional hook over the authentication schemes of this deployment. ASP.NET Core's own
    /// AddAuthentication is already the provider-agnostic seam - whatever the identity provider turns out to
    /// be, it ends at a ClaimsPrincipal - so this service defines no abstraction of its own over it.
    /// Leaving it null registers no scheme and the service runs unauthenticated, which is what a deployment
    /// behind a private network boundary wants. See <see cref="Tenancy.TenancySettings.ClaimType"/> for the
    /// other half: where the tenant is read from once there is a principal to read it off.
    /// </param>
    public static IServiceCollection AddApiServices(this IServiceCollection services,
        Action<AuthenticationBuilder>? configureAuthentication = null)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        //picks up every validator next to the feature it guards, under IK.Imager.Api/Features
        services.AddValidatorsFromAssembly(typeof(ApiServiceCollectionExtensions).Assembly);

        if (configureAuthentication != null)
        {
            configureAuthentication(services.AddAuthentication());
            services.AddAuthorization();
        }

        return services;
    }
}
