using System.Threading.Tasks;

namespace IK.Imager.EventBus.Abstractions
{
    public interface IEventBus
    {
        Task Publish<TIntegrationEvent>(string topicName, TIntegrationEvent iEvent)
            where TIntegrationEvent : IntegrationEvent;

        Task Subscribe<T>(string topicName, string subscriptionName, IIntegrationEventHandler<T> handler)
            where T : IntegrationEvent;
    }
}