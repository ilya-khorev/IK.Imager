#pragma warning disable CS1591
namespace IK.Imager.Api.IntegrationEvents.Events
{
    public class ImageMetadataDeletedIntegrationEvent
    {
        public string ImageId { get; set; }
        public string ImageName { get; set; }
        public string[] ThumbnailNames { get; set; }
    }
}