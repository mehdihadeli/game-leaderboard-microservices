using LeaderBoard.SharedKernel.Contracts.Data.EventStore.Projections;

namespace LeaderBoard.SharedKernel.Contracts.Domain;

public interface IAggregate: IAggregate<Guid>
{
}

public interface IAggregate<out T>: IProjection
{
    T Id { get; }
    int Version { get; }

    object[] DequeueUncommittedEvents();
}
