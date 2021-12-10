using System.IO;
using IK.Imager.Core.Abstractions.Models;
using MediatR;

#pragma warning disable 1591

namespace IK.Imager.Core.Commands;

public record UploadImageCommand(Stream ImageStream, string ImageGroup) : IRequest<ImageInfo>;