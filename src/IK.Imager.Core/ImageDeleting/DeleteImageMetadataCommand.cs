using MediatR;

#pragma warning disable 8632
#pragma warning disable 1591

namespace IK.Imager.Core.ImageDeleting;

public record DeleteImageMetadataCommand(string ImageId, string? ImageGroup) : IRequest<bool>;
