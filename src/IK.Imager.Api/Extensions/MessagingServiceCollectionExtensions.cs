using System;
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
    public const string ServiceBusTransportPath = "ServiceBus:Transport";

    /// <summary>
    /// The <see cref="ServiceBusTransportPath"/> value that swaps Azure Service Bus for MassTransit's
    /// in-memory transport. Anything else - including the absent setting - keeps Azure Service Bus.
    ///
    /// Azure Service Bus is the one dependency of this service with no emulator MassTransit 8 can drive:
    /// the emulator only grew an administration API in 2026, and MassTransit only speaks to it from v9,
    /// which is commercially licensed. So the API integration tests select the in-memory transport here,
    /// which also makes the service runnable locally without a real Service Bus namespace.
    ///
    /// In-memory is a single-process transport with no persistence - it is a test and local-development
    /// option, never a deployment one.
    /// </summary>
    public const string InMemoryTransport = "InMemory";

    /// <summary>
    /// Registers everything that carries integration events over Azure Service Bus - the topic configuration,
    /// the MassTransit bus with its consumers, and the domain event handlers that publish onto it.
    /// </summary>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <param name="configuration">The configuration root - this module locates its own sections within it.</param>
    public static IServiceCollection AddIntegrationEventMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TopicsSettings>(configuration.GetSection(TopicsSectionName));

        //what the core announces becomes a message on the bus registered just below - so it is registered
        //here rather than in AddImagerCore, which has no way to publish anything
        services.AddScoped<IImageEvents, ImageEventPublisher>();

        var useInMemoryTransport = string.Equals(configuration.GetValue<string>(ServiceBusTransportPath),
            InMemoryTransport, StringComparison.OrdinalIgnoreCase);

        services.AddMassTransit(x =>
        {
            x.AddConsumers(typeof(MessagingServiceCollectionExtensions).Assembly);

            //the consumers, the events and the publish/consume path are the same either way - only the
            //transport underneath them differs, so the entity naming below has no in-memory equivalent
            if (useInMemoryTransport)
            {
                x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
                return;
            }

            x.UsingAzureServiceBus((context, cfg) =>
            {
                cfg.Host(configuration.GetValue<string>(ServiceBusConnectionStringPath));

                var topicsSettings = context.GetRequiredService<IOptions<TopicsSettings>>();

                cfg.Message<OriginalImageUploadedIntegrationEvent>(c =>
                    c.SetEntityName(topicsSettings.Value.UploadedImagesTopicName));
                cfg.Message<ImageMetadataDeletedIntegrationEvent>(c =>
                    c.SetEntityName(topicsSettings.Value.DeletedImagesTopicName));

                cfg.ConcurrentMessageLimit = topicsSettings.Value.MaxConcurrentCalls;

                cfg.SubscriptionEndpoint<OriginalImageUploadedIntegrationEvent>(topicsSettings.Value.SubscriptionName,
                    configurator =>
                    {
                        configurator.ConfigureConsumer<CreateThumbnailsConsumer>(context);
                    });
                cfg.SubscriptionEndpoint<ImageMetadataDeletedIntegrationEvent>(topicsSettings.Value.SubscriptionName,
                    configurator =>
                    {
                        configurator.ConfigureConsumer<RemoveImageFilesConsumer>(context);
                    });
            });
        });

        return services;
    }
}
