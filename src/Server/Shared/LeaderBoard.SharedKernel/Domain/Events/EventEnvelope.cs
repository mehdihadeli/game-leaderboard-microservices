using LeaderBoard.SharedKernel.Contracts.Domain.Events;

namespace LeaderBoard.SharedKernel.Domain.Events;

public record EventEnvelope<T>(T Data, EventMetadata Metadata) : IEventEnvelope<T>
    where T : IDomainEvent
{
    object IEventEnvelope.Data => Data;
}

public record EventEnvelope(object Data, EventMetadata Metadata) : IEventEnvelope;

public static class EventEnvelopeFactory
{
    public static IEventEnvelope From(object data, EventMetadata metadata)
    {
        //TODO: Get rid of reflection!
        var type = typeof(EventEnvelope<>).MakeGenericType(data.GetType());
        return (IEventEnvelope)Activator.CreateInstance(type, data, metadata)!;
    }
}
