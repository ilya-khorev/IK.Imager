using MediatR;

namespace IK.Imager.Core.Abstractions.ImageDeleting;

public record ImageDeletedDomainEvent(string ImageId, string ImageName, string[] ThumbnailNames) : INotification;