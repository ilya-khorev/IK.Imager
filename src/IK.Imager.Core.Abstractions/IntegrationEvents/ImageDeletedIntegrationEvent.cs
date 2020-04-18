using IK.Imager.EventBus.Abstractions;

namespace IK.Imager.Core.Abstractions.IntegrationEvents
{
    public class ImageDeletedIntegrationEvent: IntegrationEvent
    {
        public override string MessageId => ImageId;
        public string ImageId { get; set; }
        public string ImageName { get; set; }
        public string[] ThumbnailNames { get; set; }
    }
}