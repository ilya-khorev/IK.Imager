using MediatR;

namespace IK.Imager.Core.Abstractions.ImageRemoving;

public record ImageRemovedDomainEvent(string ImageId, string ImageName, string[] ThumbnailNames) : INotification;