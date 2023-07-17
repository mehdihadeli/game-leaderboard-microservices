namespace LeaderBoard.SharedKernel.Contracts.Domain.Events;

public interface IInternalEventBus
{
    Task Publish(IEventEnvelope @event, CancellationToken ct);
    Task Publish<T>(IEventEnvelope<T> @event, CancellationToken ct)
        where T : IDomainEvent;
    Task Publish(IEvent @event, CancellationToken ct);
}
