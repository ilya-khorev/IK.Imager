using IK.Imager.Api.Contract;
using MediatR;
#pragma warning disable 1591

namespace IK.Imager.Api.Queries
{
    public record RequestImagesQuery(string[] ImageIds, string ImageGroup) : IRequest<ImagesSearchResult>;
}