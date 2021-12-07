using System.IO;
using IK.Imager.Api.Contract;
using MediatR;
#pragma warning disable 1591

namespace IK.Imager.Api.Commands
{
    public record UploadImageCommand(Stream ImageStream, string ImageGroup) : IRequest<ImageInfo>;
}