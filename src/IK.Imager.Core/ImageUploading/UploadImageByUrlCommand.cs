using IK.Imager.Core.Abstractions.Models;
using MediatR;

#pragma warning disable 1591

namespace IK.Imager.Core.ImageUploading;

public record UploadImageByUrlCommand(string ImageUrl, string ImageGroup) : IRequest<ImageInfo>;