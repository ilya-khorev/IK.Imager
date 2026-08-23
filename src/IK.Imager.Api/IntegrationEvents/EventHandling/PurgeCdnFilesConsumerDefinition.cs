using System;
using MassTransit;
// ReSharper disable ClassNeverInstantiated.Global

#pragma warning disable 1591

namespace IK.Imager.Api.IntegrationEvents.EventHandling;

/// <summary>
/// Retries a failed CDN purge before it is dead lettered.
/// </summary>
/// <remarks>
/// Every CDN rate limits purging, so the usual failure is a 429 that succeeds moments later. Without a
/// retry policy the message goes straight through its delivery attempts and is dead lettered in
/// milliseconds. Backing off is safe because purging is idempotent - a retry purges the same uris again.
///
/// A consumer definition rather than endpoint configuration, so it also applies on the in-memory transport
/// the tests run on.
/// </remarks>
public class PurgeCdnFilesConsumerDefinition : ConsumerDefinition<PurgeCdnFilesConsumer>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<PurgeCdnFilesConsumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r =>
            r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(5)));
    }
}
