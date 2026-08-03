#pragma warning disable CS1591
namespace IK.Imager.Api.IntegrationEvents.Events;

public class ImageMetadataDeletedIntegrationEvent
{
    public string ImageId { get; set; } = null!;
    public string ImageName { get; set; } = null!;
    public string[] ThumbnailNames { get; set; } = [];
}