using IK.Imager.Core.Abstractions.Messaging;

namespace IK.Imager.Core.ImageUploading;

public record ImageUploadedDomainEvent(string ImageId, string ImageGroup): IDomainEvent;