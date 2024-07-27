using LeaderBoard.SharedKernel.Contracts.Domain.Events;

namespace LeaderBoard.SharedKernel.Contracts.Data.EventStore.Projections;

// https://zimarev.com/blog/event-sourcing/projections/
// https://event-driven.io/en/how_to_do_events_projections_with_entity_framework/
// https://www.youtube.com/watch?v=bTRjO6JK4Ws
// https://www.eventstore.com/blog/event-sourcing-and-cqrs
public interface IReadProjection
{
    Task ProjectAsync<TEvent>(IStreamEvent<TEvent> streamEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}

public interface IReadProjection<in TEvent>
    where TEvent : IDomainEvent
{
    Task ProjectAsync(IStreamEvent<TEvent> streamEvent, CancellationToken cancellationToken = default);
}
