using LeaderBoard.SharedKernel.Contracts.Domain.Events;

namespace LeaderBoard.SharedKernel.Contracts.Data.EventStore.Projections;

public interface IReadProjectionPublisher
{
    Task PublishAsync(IStreamEvent streamEvent, CancellationToken cancellationToken = default);

    Task PublishAsync<T>(IStreamEvent<T> streamEvent, CancellationToken cancellationToken = default)
        where T : IDomainEvent;
}
