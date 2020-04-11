using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.IntegrationEvents;
using IK.Imager.EventBus.Abstractions;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IK.Imager.BackgroundService
{
    public class BackgroundTasks: Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly ILogger<BackgroundTasks> _logger;
        private readonly IEventBus _eventBus;
        private readonly TelemetryClient _telemetryClient;
        private readonly IOptions<TopicsConfiguration> _topicsConfiguration;
        private readonly IIntegrationEventHandler<OriginalImageUploadedIntegrationEvent> _imageUploadedEventHandler;
        private readonly IIntegrationEventHandler<ImageDeletedIntegrationEvent> _imageDeletedEventHandler;

        public BackgroundTasks(ILogger<BackgroundTasks> logger, IEventBus eventBus, TelemetryClient telemetryClient, IOptions<TopicsConfiguration> topicsConfiguration, 
            IIntegrationEventHandler<OriginalImageUploadedIntegrationEvent> imageUploadedEventHandler, IIntegrationEventHandler<ImageDeletedIntegrationEvent> imageDeletedEventHandler)
        {
            _logger = logger;
            _eventBus = eventBus;
            _telemetryClient = telemetryClient;
            _topicsConfiguration = topicsConfiguration;
            _imageUploadedEventHandler = imageUploadedEventHandler;
            _imageDeletedEventHandler = imageDeletedEventHandler;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _telemetryClient.TrackEvent("Starting background tasks...");

            const int maxConcurrentCalls = 5; //todo move to config
                
            await _eventBus.Subscribe(_topicsConfiguration.Value.UploadedImagesTopicName, _topicsConfiguration.Value.SubscriptionName, _imageUploadedEventHandler, maxConcurrentCalls);
            await _eventBus.Subscribe(_topicsConfiguration.Value.DeletedImagesTopicName, _topicsConfiguration.Value.SubscriptionName, _imageDeletedEventHandler, maxConcurrentCalls);

            _logger.LogInformation("Subscribed on events from topics.");
        }
    }
}