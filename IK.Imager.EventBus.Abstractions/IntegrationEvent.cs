namespace IK.Imager.EventBus.Abstractions
{
    public abstract class IntegrationEvent
    {
        public abstract string MessageId { get; }
    }
}