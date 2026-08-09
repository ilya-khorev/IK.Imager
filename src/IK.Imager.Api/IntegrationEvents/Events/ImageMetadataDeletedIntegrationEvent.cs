#pragma warning disable CS1591
namespace IK.Imager.Api.IntegrationEvents.Events;

public record ImageMetadataDeletedIntegrationEvent(string ImageId, string ImageName, string[] ThumbnailNames);