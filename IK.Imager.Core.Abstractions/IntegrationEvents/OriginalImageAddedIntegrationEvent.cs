using IK.Imager.EventBus.Abstractions;

namespace IK.Imager.Core.Abstractions.IntegrationEvents
{
    public class OriginalImageAddedIntegrationEvent: IntegrationEvent
    {
        public override string MessageId { get; }
    }
}