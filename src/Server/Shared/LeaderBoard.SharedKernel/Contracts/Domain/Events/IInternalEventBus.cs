using LeaderBoard.SharedKernel.Contracts.Data.EventStore;

namespace LeaderBoard.SharedKernel.Contracts.Domain.Events;

public interface IInternalEventBus
{
    Task Publish(IStreamEvent @event, CancellationToken ct);
    Task Publish<T>(IStreamEvent<T> @event, CancellationToken ct)
        where T : IDomainEvent;
    Task Publish(IEvent @event, CancellationToken ct);
}
