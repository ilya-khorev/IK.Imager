using MediatR;

namespace IK.Imager.Core.Abstractions.ImageUploading;

public record ImageUploadedDomainEvent(string ImageId, string ImageGroup): INotification;