using LeaderBoard.SharedKernel.Contracts.Data.EventStore.Projections;
using LeaderBoard.SharedKernel.Contracts.Domain;
using LeaderBoard.SharedKernel.Contracts.Domain.Events;
using LeaderBoard.SharedKernel.Contracts.Domain.EventSourcing;

namespace LeaderBoard.SharedKernel.Contracts.Data.EventStore;

public interface IHaveEventSourcingAggregate
    : IHaveAggregateStateProjection,
        IHaveAggregate,
        IHaveEventSourcedAggregateVersion
{
    /// <summary>
    /// Loads the current state of the aggregate from a list of events.
    /// </summary>
    /// <param name="history">Domain events from the aggregate stream.</param>
    void LoadFromHistory(IEnumerable<IDomainEvent> history);
}
