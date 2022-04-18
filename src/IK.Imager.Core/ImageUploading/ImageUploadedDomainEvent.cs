using MediatR;

namespace IK.Imager.Core.ImageUploading;

public record ImageUploadedDomainEvent(string ImageId, string ImageGroup): INotification;