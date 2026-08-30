#pragma warning disable CS1591
namespace IK.Imager.Api.IntegrationEvents.Events;

public record OriginalImageUploadedIntegrationEvent(string TenantId, string ImageId);
