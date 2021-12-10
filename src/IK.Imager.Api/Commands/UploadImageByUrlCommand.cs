using IK.Imager.Api.Contract;
using MediatR;
#pragma warning disable 1591

namespace IK.Imager.Api.Commands
{
    public record UploadImageByUrlCommand(string ImageUrl, string ImageGroup) : IRequest<ImageInfo>;
}