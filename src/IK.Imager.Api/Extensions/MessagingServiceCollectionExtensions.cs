using System;
using Azure.Core;
using Azure.Identity;
using IK.Imager.Api.IntegrationEvents;
using IK.Imager.Api.IntegrationEvents.EventHandling;
using IK.Imager.Api.IntegrationEvents.Events;
using IK.Imager.Core.Abstractions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

#pragma warning disable 1591

namespace IK.Imager.Api.Extensions;

public static class MessagingServiceCollectionExtensions
{
    public const string TopicsSectionName = "Topics";
    public const string ServiceBusConnectionStringPath = "ServiceBus:ConnectionString";
    public const string ServiceBusTransportPath = "ServiceBus:Transport";

    /// <summary>
    /// Namespace host, e.g. mynamespace.servicebus.windows.net. Setting it reaches the namespace with
    /// DefaultAzureCredential instead of the connection string.
    /// </summary>
    public const string ServiceBusNamespacePath = "ServiceBus:FullyQualifiedNamespace";

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
    /// <remarks>
    /// <see cref="ServiceBusNamespacePath"/> is what picks the authentication: set it and the namespace is
    /// reached with <see cref="DefaultAzureCredential"/>, leave it empty and the connection string is used.
    /// The namespace wins when both are set.
    /// </remarks>
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

        var serviceBusNamespace = configuration.GetValue<string>(ServiceBusNamespacePath);
        if (!useInMemoryTransport && !string.IsNullOrWhiteSpace(serviceBusNamespace))
            //one credential for every Azure client in the process - it caches the tokens it fetches.
            //TryAdd, so a host that wants a credential of its own can register it before this runs.
            services.TryAddSingleton<TokenCredential>(_ => new DefaultAzureCredential());

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
                ConfigureHost(cfg, context, configuration, serviceBusNamespace);

                var topicsSettings = context.GetRequiredService<IOptions<TopicsSettings>>();

                cfg.Message<OriginalImageUploadedIntegrationEvent>(c =>
                    c.SetEntityName(topicsSettings.Value.UploadedImagesTopicName));
                cfg.Message<ImageMetadataDeletedIntegrationEvent>(c =>
                    c.SetEntityName(topicsSettings.Value.DeletedImagesTopicName));
                cfg.Message<ImageFilesDeletedIntegrationEvent>(c =>
                    c.SetEntityName(topicsSettings.Value.DeletedImageFilesTopicName));

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
                cfg.SubscriptionEndpoint<ImageFilesDeletedIntegrationEvent>(topicsSettings.Value.SubscriptionName,
                    configurator =>
                    {
                        configurator.ConfigureConsumer<PurgeCdnFilesConsumer>(context);
                    });
            });
        });

        return services;
    }

    //MassTransit takes the namespace as a uri in its own format when it is handed a credential, and reads
    //the namespace out of the connection string otherwise
    private static void ConfigureHost(IServiceBusBusFactoryConfigurator cfg, IServiceProvider provider,
        IConfiguration configuration, string? serviceBusNamespace)
    {
        if (!string.IsNullOrWhiteSpace(serviceBusNamespace))
        {
            var credential = provider.GetRequiredService<TokenCredential>();
            cfg.Host(new Uri($"sb://{serviceBusNamespace}"), host => host.TokenCredential = credential);
            return;
        }

        var connectionString = configuration.GetValue<string>(ServiceBusConnectionStringPath);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"Service Bus is not configured. Set '{ServiceBusNamespacePath}' to reach the namespace " +
                $"with a managed identity, or '{ServiceBusConnectionStringPath}' to reach it with a key. " +
                $"Set '{ServiceBusTransportPath}' to '{InMemoryTransport}' to run without a namespace.");

        cfg.Host(connectionString);
    }
}
