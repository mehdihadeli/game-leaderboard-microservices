using LeaderBoard.SharedKernel.Contracts.Domain.Events;

namespace LeaderBoard.SharedKernel.Contracts.Data.EventStore;

public interface IStreamEvent : IEvent
{
    public IDomainEvent Data { get; }

    public IStreamEventMetadata? Metadata { get; }
}

public interface IStreamEvent<out T> : IStreamEvent
    where T : IDomainEvent
{
    public new T Data { get; }
}
