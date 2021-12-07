using MediatR;
#pragma warning disable 8632
#pragma warning disable 1591

namespace IK.Imager.Api.Commands
{
    public record RemoveImageCommand(string ImageId, string? ImageGroup) : IRequest<RemoveImageResult>;

    public record RemoveImageResult(string ImageId, string ImageName, string[] ThumbnailNames);
}