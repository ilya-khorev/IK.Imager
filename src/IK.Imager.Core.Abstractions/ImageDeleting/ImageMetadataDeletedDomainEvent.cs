using MediatR;

namespace IK.Imager.Core.Abstractions.ImageDeleting;

public record ImageMetadataDeletedDomainEvent(string ImageId, string ImageName, string[] ThumbnailNames) : INotification;