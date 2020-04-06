using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.IntegrationEvents;
using IK.Imager.EventBus.Abstractions;

namespace IK.Imager.BackgroundService.Handlers
{
    public class GenerateThumbnailsHandler: IIntegrationEventHandler<OriginalImageUploadedIntegrationEvent>
    {
        public Task Handle(OriginalImageUploadedIntegrationEvent iEvent)
        {
            throw new System.NotImplementedException();
        }
    }
}