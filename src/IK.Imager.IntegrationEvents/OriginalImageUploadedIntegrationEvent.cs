using IK.Imager.EventBus.Abstractions;

namespace IK.Imager.IntegrationEvents
{
    public class OriginalImageUploadedIntegrationEvent: IntegrationEvent
    {
        public override string MessageId => ImageId + ImageGroup;
        public string ImageId { get; set; }
        public string ImageGroup { get; set; }
    }
}