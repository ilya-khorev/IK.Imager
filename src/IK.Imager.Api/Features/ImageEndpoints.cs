using IK.Imager.Api.Features.Delete;
using IK.Imager.Api.Features.Lookup;
using IK.Imager.Api.Features.Upload;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

#pragma warning disable 1591

namespace IK.Imager.Api.Features;

public static class ImageEndpoints
{
    private const string ImagesPrefix = "/images";

    /// <summary>
    /// Maps every feature onto the /Images group. A feature owns its routes, its request models and its
    /// validators - add an endpoint to the feature it belongs to, never here.
    /// </summary>
    public static IEndpointRouteBuilder MapImageEndpoints(this IEndpointRouteBuilder app)
    {
        //the group keeps the single "Images" tag the controller used to give the whole API in Swagger UI
        var images = app.MapGroup(ImagesPrefix)
            .WithTags("Images");

        images.MapUploadEndpoints();
        images.MapLookupEndpoints();
        images.MapDeleteEndpoints();

        return app;
    }
}
