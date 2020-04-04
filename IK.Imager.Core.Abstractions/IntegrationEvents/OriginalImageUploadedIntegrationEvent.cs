using IK.Imager.EventBus.Abstractions;

namespace IK.Imager.Core.Abstractions.IntegrationEvents
{
    public class OriginalImageUploadedIntegrationEvent: IntegrationEvent
    {
        public override string MessageId { get; set; }
        public string ImageId { get; set; }
        public string PartitionKey { get; set; }
    }
}