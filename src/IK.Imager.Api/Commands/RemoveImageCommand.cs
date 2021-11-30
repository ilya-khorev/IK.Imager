using MediatR;

namespace IK.Imager.Api.Commands
{
    public record RemoveImageCommand(string ImageId, string? ImageGroup) : IRequest<RemoveImageResult>;

    public record RemoveImageResult(string ImageId, string ImageName, string[] ThumbnailNames);
}