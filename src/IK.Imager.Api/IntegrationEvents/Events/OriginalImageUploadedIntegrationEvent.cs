#pragma warning disable CS1591
namespace IK.Imager.Api.IntegrationEvents.Events;

public record OriginalImageUploadedIntegrationEvent(string ImageId, string ImageGroup);