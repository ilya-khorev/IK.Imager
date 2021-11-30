using IK.Imager.Api.Contract;
using MediatR;

namespace IK.Imager.Api.Commands
{
    public record RequestImagesCommand(string[] ImageIds, string ImageGroup) : IRequest<ImagesSearchResult>;
}