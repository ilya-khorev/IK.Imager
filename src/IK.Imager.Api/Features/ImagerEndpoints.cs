using IK.Imager.Api.Features.ImageDeleting;
using IK.Imager.Api.Features.ImageLookup;
using IK.Imager.Api.Features.ImageUpload;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

#pragma warning disable 1591

namespace IK.Imager.Api.Features;

public static class ImagerEndpoints
{
    private const string ImagesPrefix = "/Images";

    /// <summary>
    /// Maps every feature onto the /Images group. A feature owns its routes, its request models and its
    /// validators - add an endpoint to the feature it belongs to, never here.
    /// </summary>
    public static IEndpointRouteBuilder MapImagerEndpoints(this IEndpointRouteBuilder app)
    {
        //the group keeps the single "Images" tag the controller used to give the whole API in Swagger UI
        var images = app.MapGroup(ImagesPrefix)
            .WithTags("Images");

        images.MapImageUploadEndpoints();
        images.MapImageLookupEndpoints();
        images.MapImageDeletingEndpoints();

        return app;
    }
}
