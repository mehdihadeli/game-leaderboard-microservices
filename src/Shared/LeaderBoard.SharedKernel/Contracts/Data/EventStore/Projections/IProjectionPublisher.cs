using LeaderBoard.SharedKernel.Contracts.Domain.Events;

namespace LeaderBoard.SharedKernel.Contracts.Data.EventStore.Projections;

public interface IProjectionPublisher
{
    Task PublishAsync(
        IEventEnvelope eventEnvelope,
        CancellationToken cancellationToken = default);
}
