using MediatR;

#pragma warning disable 8632
#pragma warning disable 1591

namespace IK.Imager.Core.Abstractions.ImageRemoving;

public record RemoveImageCommand(string ImageId, string? ImageGroup) : IRequest<bool>;
