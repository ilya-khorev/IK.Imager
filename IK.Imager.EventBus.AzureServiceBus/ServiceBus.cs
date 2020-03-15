using System;
using System.Threading.Tasks;
using IK.Imager.EventBus.Abstractions;

namespace IK.Imager.EventBus.AzureServiceBus
{
    public class ServiceBus : IEventBus
    {
        public Task Publish<TIntegrationEvent>(string topicName, TIntegrationEvent iEvent)
            where TIntegrationEvent : IntegrationEvent
        {
            throw new NotImplementedException();
        }

        public void Subscribe<T, TH>(string topicName, string subscriptionName) where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            throw new NotImplementedException();
        }
    }
}