using IK.Imager.Api.IntegrationEvents;
using IK.Imager.Api.IntegrationEvents.EventHandling;
using IK.Imager.Api.IntegrationEvents.Events;
using IK.Imager.Core.Abstractions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

#pragma warning disable 1591

namespace IK.Imager.Api.Extensions;

public static class MessagingServiceCollectionExtensions
{
    public const string TopicsSectionName = "Topics";
    public const string ServiceBusConnectionStringPath = "ServiceBus:ConnectionString";

    /// <summary>
    /// Registers everything that carries integration events over Azure Service Bus - the topic configuration,
    /// the MassTransit bus with its consumers, and the domain event handlers that publish onto it.
    /// </summary>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <param name="configuration">The configuration root - this module locates its own sections within it.</param>
    public static IServiceCollection AddIntegrationEventMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TopicsConfiguration>(configuration.GetSection(TopicsSectionName));

        //what the core announces becomes a message on the bus registered just below - so it is registered
        //here rather than in AddImagerCore, which has no way to publish anything
        services.AddScoped<IImageEvents, ImageEvents>();

        services.AddMassTransit(x =>
        {
            x.AddConsumers(typeof(MessagingServiceCollectionExtensions).Assembly);
            x.UsingAzureServiceBus((context, cfg) =>
            {
                cfg.Host(configuration.GetValue<string>(ServiceBusConnectionStringPath));

                var topicsConfiguration = context.GetRequiredService<IOptions<TopicsConfiguration>>();

                cfg.Message<OriginalImageUploadedIntegrationEvent>(c =>
                    c.SetEntityName(topicsConfiguration.Value.UploadedImagesTopicName));
                cfg.Message<ImageMetadataDeletedIntegrationEvent>(c =>
                    c.SetEntityName(topicsConfiguration.Value.DeletedImagesTopicName));

                cfg.ConcurrentMessageLimit = topicsConfiguration.Value.MaxConcurrentCalls;

                cfg.SubscriptionEndpoint<OriginalImageUploadedIntegrationEvent>(topicsConfiguration.Value.SubscriptionName,
                    configurator =>
                    {
                        configurator.ConfigureConsumer<CreateThumbnailsHandler>(context);
                    });
                cfg.SubscriptionEndpoint<ImageMetadataDeletedIntegrationEvent>(topicsConfiguration.Value.SubscriptionName,
                    configurator =>
                    {
                        configurator.ConfigureConsumer<RemoveImageFilesHandler>(context);
                    });
            });
        });

        return services;
    }
}
