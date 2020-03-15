using System.Threading.Tasks;

namespace IK.Imager.EventBus.Abstractions
{
    public interface IEventBus
    {
        Task Publish<TIntegrationEvent>(string topicName, TIntegrationEvent iEvent)
            where TIntegrationEvent : IntegrationEvent;
        
        void Subscribe<T, TH>(string topicName, string subscriptionName)
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;
    }
}