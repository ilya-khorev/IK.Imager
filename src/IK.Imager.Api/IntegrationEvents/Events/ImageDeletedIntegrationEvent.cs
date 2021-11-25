namespace IK.Imager.Api.IntegrationEvents.Events
{
    public class ImageDeletedIntegrationEvent
    {
        public string ImageId { get; set; }
        public string ImageName { get; set; }
        public string[] ThumbnailNames { get; set; }
    }
}