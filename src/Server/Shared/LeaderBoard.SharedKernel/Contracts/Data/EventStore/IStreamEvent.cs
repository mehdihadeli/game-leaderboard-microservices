using LeaderBoard.SharedKernel.Contracts.Domain.Events;

namespace LeaderBoard.SharedKernel.Contracts.Data.EventStore;

public interface IStreamEvent
{
    object Data { get; }
    IStreamEventMetadata Metadata { get; init; }
}

public interface IStreamEvent<out T> : IStreamEvent
where T : IDomainEvent
{
    new T Data { get; }
}
