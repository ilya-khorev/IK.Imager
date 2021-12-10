using MediatR;

#pragma warning disable 8632
#pragma warning disable 1591

namespace IK.Imager.Core.Abstractions.ImageDeleting;

public record DeleteImageCommand(string ImageId, string? ImageGroup) : IRequest<bool>;
