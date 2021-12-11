using MediatR;

namespace IK.Imager.Core.Abstractions.Thumbnails;

public record CreateThumbnailsCommand(string ImageId, string ImageGroup) : IRequest;
