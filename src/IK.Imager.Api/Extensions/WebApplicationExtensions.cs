using IK.Imager.Api.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

#pragma warning disable 1591

namespace IK.Imager.Api.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Builds the HTTP pipeline - developer diagnostics, the OpenAPI document and its Swagger UI,
    /// the Service Fabric middleware, the controllers and the health endpoints.
    /// </summary>
    public static WebApplication UseImagerPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
            app.UseDeveloperExceptionPage();

        app.UseOpenApiDocumentation();

        app.UseMiddleware<ServiceFabricResourceNotFoundMiddleware>();

        app.MapControllers();
        app.MapImagerHealthChecks();

        return app;
    }
}
