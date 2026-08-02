using System.Threading;
using System.Threading.Tasks;

namespace IK.Imager.Core.Abstractions.Messaging
{
    /// <summary>
    /// Handles a command that produces a result.
    /// </summary>
    public interface ICommandHandler<in TCommand, TResult>
    {
        Task<TResult> Handle(TCommand command, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Handles a command that does not produce a result.
    /// </summary>
    public interface ICommandHandler<in TCommand>
    {
        Task Handle(TCommand command, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Handles a query that produces a result.
    /// </summary>
    public interface IQueryHandler<in TQuery, TResult>
    {
        Task<TResult> Handle(TQuery query, CancellationToken cancellationToken);
    }
}
