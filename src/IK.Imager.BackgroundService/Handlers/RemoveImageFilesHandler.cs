using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.IntegrationEvents;
using IK.Imager.EventBus.Abstractions;

namespace IK.Imager.BackgroundService.Handlers
{
    public class RemoveImageFilesHandler: IIntegrationEventHandler<ImageDeletedIntegrationEvent>
    {
        public Task Handle(ImageDeletedIntegrationEvent iEvent)
        {
            throw new System.NotImplementedException();
        }
    }
}