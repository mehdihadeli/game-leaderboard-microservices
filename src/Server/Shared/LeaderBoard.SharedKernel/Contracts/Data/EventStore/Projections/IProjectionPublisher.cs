namespace LeaderBoard.SharedKernel.Contracts.Data.EventStore.Projections;

public interface IProjectionPublisher
{
    Task PublishAsync(IStreamEvent eventEnvelope, CancellationToken cancellationToken = default);
}
