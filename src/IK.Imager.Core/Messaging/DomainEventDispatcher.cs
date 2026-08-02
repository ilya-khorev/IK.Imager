using System;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace IK.Imager.Core.Messaging;

/// <summary>
/// Resolves every <see cref="IDomainEventHandler{TDomainEvent}"/> registered for the event type
/// and invokes them sequentially.
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task Publish<TDomainEvent>(TDomainEvent domainEvent, CancellationToken cancellationToken = default)
        where TDomainEvent : IDomainEvent
    {
        foreach (var handler in _serviceProvider.GetServices<IDomainEventHandler<TDomainEvent>>())
            await handler.Handle(domainEvent, cancellationToken);
    }
}
