using LeaderBoard.SharedKernel.Contracts.Domain.Events;

namespace LeaderBoard.SharedKernel.Core.Data.EventStore;

public class EventStoreDomainEventAccessor : IDomainEventsAccessor
{
    private readonly IAggregatesDomainEventsRequestStore _aggregatesDomainEventsStore;

    public EventStoreDomainEventAccessor(IAggregatesDomainEventsRequestStore aggregatesDomainEventsStore)
    {
        _aggregatesDomainEventsStore = aggregatesDomainEventsStore;
    }

    public IReadOnlyList<IDomainEvent> UnCommittedDomainEvents =>
        _aggregatesDomainEventsStore.GetAllUncommittedEvents();
}
