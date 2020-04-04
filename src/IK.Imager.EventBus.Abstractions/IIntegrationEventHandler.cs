using System.Threading.Tasks;

namespace IK.Imager.EventBus.Abstractions
{
    public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler 
        where TIntegrationEvent: IntegrationEvent
    {
        Task Handle(TIntegrationEvent iEvent);
    }

    public interface IIntegrationEventHandler
    {
    }
}