using IK.Imager.Api.Features;
using IK.Imager.Api.Middleware;
using Microsoft.AspNetCore.Builder;

#pragma warning disable 1591

namespace IK.Imager.Api.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Builds the HTTP pipeline - the exception handler, the OpenAPI document and its Swagger UI,
    /// the Service Fabric middleware, the feature endpoints and the health endpoints.
    /// </summary>
    public static WebApplication UseImagerPipeline(this WebApplication app)
    {
        //outermost, so it covers the endpoints and the middleware below alike. There is no developer
        //exception page: the handler already returns the full exception in the Development environment,
        //and it did so for every action exception under MVC too.
        app.UseExceptionHandler();

        app.UseOpenApiDocumentation();

        app.UseMiddleware<ServiceFabricResourceNotFoundMiddleware>();

        app.MapImagerEndpoints();
        app.MapImagerHealthChecks();

        return app;
    }
}
