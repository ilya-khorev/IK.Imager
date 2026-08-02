using System.Threading;
using System.Threading.Tasks;

namespace IK.Imager.Core.Abstractions.Messaging
{
    /// <summary>
    /// Marker interface for an in-process event, raised by a handler once a piece of work has been committed.
    /// Domain events never leave the process - the API layer is the one translating them into integration events.
    /// </summary>
    public interface IDomainEvent
    {
    }

    /// <summary>
    /// Handles a domain event. Several handlers may be registered for the same event.
    /// </summary>
    public interface IDomainEventHandler<in TDomainEvent> where TDomainEvent : IDomainEvent
    {
        Task Handle(TDomainEvent domainEvent, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Publishes a domain event to every registered <see cref="IDomainEventHandler{TDomainEvent}"/>.
    /// </summary>
    public interface IDomainEventDispatcher
    {
        Task Publish<TDomainEvent>(TDomainEvent domainEvent, CancellationToken cancellationToken = default)
            where TDomainEvent : IDomainEvent;
    }
}
