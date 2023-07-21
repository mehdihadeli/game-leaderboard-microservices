namespace LeaderBoard.SharedKernel.Contracts.Domain.Events;

public interface IEventEnvelope
{
    object Data { get; }
    EventMetadata Metadata { get; init; }
}

public interface IEventEnvelope<out T> : IEventEnvelope
    where T : IDomainEvent
{
    new T Data { get; }
}
