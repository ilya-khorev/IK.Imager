using MediatR;

namespace IK.Imager.Core.ImageDeleting;

public record ImageMetadataDeletedDomainEvent(string ImageId, string ImageName, string[] ThumbnailNames) : INotification;