using FluentValidation;
using IK.Imager.Api.ExceptionHandling;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable 1591

namespace IK.Imager.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the minimal API endpoints need - the exception handler that turns an unhandled
    /// exception into a response, and the request validators the endpoint filters resolve.
    /// </summary>
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        //picks up every validator next to the feature it guards, under IK.Imager.Api/Features
        services.AddValidatorsFromAssembly(typeof(ApiServiceCollectionExtensions).Assembly);

        return services;
    }
}
