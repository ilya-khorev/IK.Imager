using System.Threading.Tasks;
using IK.Imager.Api.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable 1591

namespace IK.Imager.Api.Extensions;

public static class OpenApiServiceCollectionExtensions
{
    internal const string ApiTitle = "IK.Imager API";
    internal const string DocumentName = "v1";
    internal const string CurrentVersion = "v1.0";

    private const string DocumentRoute = $"/openapi/{DocumentName}.json";

    /// <summary>
    /// Registers the OpenAPI document generator shipped with ASP.NET Core. The endpoint descriptions and
    /// the schema descriptions come from the XML documentation of this assembly and of IK.Imager.Api.Contract,
    /// which Microsoft.AspNetCore.OpenApi picks up at compile time; the constraints declared by the
    /// FluentValidation validators are applied by <see cref="FluentValidationRules"/>.
    /// </summary>
    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(DocumentName, options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = ApiTitle;
                document.Info.Version = CurrentVersion;
                return Task.CompletedTask;
            });

            //the form-bound upload model needs nothing special: a minimal API endpoint keeps it a type all
            //the way into the document, so it picks up its constraints and its XML summaries here like any
            //body-bound model. Under MVC it arrived flattened into fields and had to be repaired by hand.
            options.AddSchemaTransformer(new FluentValidationSchemaTransformer());

            //the tenant travels in a header rather than in a request model, so nothing else in the document
            //would mention it
            options.AddOperationTransformer<TenantHeaderOperationTransformer>();
        });

        return services;
    }

    /// <summary>
    /// Serves the OpenAPI document and hosts the Swagger UI at the root path.
    /// </summary>
    public static WebApplication UseOpenApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi();

        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint(DocumentRoute, ApiTitle);
            c.RoutePrefix = string.Empty;
        });

        return app;
    }
}
