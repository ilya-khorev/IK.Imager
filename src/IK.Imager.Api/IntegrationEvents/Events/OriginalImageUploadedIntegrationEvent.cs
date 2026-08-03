#pragma warning disable CS1591
namespace IK.Imager.Api.IntegrationEvents.Events;

public class OriginalImageUploadedIntegrationEvent
{
    public string ImageId { get; set; } = null!;
    public string ImageGroup { get; set; } = null!;
}