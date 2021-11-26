using System.IO;
using IK.Imager.Api.Contract;
using MediatR;

namespace IK.Imager.Api.Commands
{
    public record UploadImageCommand(Stream ImageStream, string ImageGroup) : IRequest<ImageInfo>;
}