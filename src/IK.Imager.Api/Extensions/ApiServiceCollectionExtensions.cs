using FluentValidation;
using IK.Imager.Api.Filters;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable 1591

namespace IK.Imager.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    /// <summary>
    /// Registers the MVC layer - controllers, the global filters, and the request validators
    /// from IK.Imager.Api/Validations.
    /// </summary>
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add(typeof(GlobalExceptionFilter));
            options.Filters.Add(typeof(FluentValidationActionFilter));
        });

        services.AddValidatorsFromAssembly(typeof(ApiServiceCollectionExtensions).Assembly);

        return services;
    }
}
