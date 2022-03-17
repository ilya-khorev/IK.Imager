using MediatR;

namespace IK.Imager.Core.Thumbnails;

public record CreateThumbnailsCommand(string ImageId, string ImageGroup) : IRequest;
