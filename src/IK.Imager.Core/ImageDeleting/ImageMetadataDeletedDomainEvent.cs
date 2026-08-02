using IK.Imager.Core.Abstractions.Messaging;

namespace IK.Imager.Core.ImageDeleting;

public record ImageMetadataDeletedDomainEvent(string ImageId, string ImageName, string[] ThumbnailNames) : IDomainEvent;