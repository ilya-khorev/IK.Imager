using System;
using System.IO;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

#pragma warning disable 1591

namespace IK.Imager.Api.Extensions;

public static class SwaggerServiceCollectionExtensions
{
    internal const string ApiTitle = "IK.Imager API";
    internal const string CurrentVersion = "v1.0";

    /// <summary>
    /// Registers the Swagger generator, feeding it the XML documentation of every IK.Imager assembly
    /// and the constraints declared by the FluentValidation validators.
    /// </summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(CurrentVersion, new OpenApiInfo {Title = ApiTitle, Version = CurrentVersion});
            foreach (var contractFile in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "IK.Imager.*.xml", SearchOption.AllDirectories))
                options.IncludeXmlComments(contractFile);
        });

        //configures the generator registered just above, hence kept together with it
        services.AddFluentValidationRulesToSwagger();

        return services;
    }

    /// <summary>
    /// Serves the Swagger document and hosts the Swagger UI at the root path.
    /// </summary>
    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint($"/swagger/{CurrentVersion}/swagger.json", ApiTitle);
            c.RoutePrefix = string.Empty;
        });

        return app;
    }
}
