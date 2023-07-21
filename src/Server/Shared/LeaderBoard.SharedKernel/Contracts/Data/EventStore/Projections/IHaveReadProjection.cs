using LeaderBoard.SharedKernel.Contracts.Domain.Events;

namespace LeaderBoard.SharedKernel.Contracts.Data.EventStore.Projections;

public interface IHaveReadProjection
{
    Task ProjectAsync<T>(IStreamEvent<T> streamEvent, CancellationToken cancellationToken = default)
        where T : IDomainEvent;
}
