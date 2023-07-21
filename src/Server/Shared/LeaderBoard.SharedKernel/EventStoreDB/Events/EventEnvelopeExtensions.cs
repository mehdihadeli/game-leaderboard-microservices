using EventStore.Client;
using LeaderBoard.SharedKernel.Contracts.Domain.Events;
using LeaderBoard.SharedKernel.Domain.Events;
using LeaderBoard.SharedKernel.EventStoreDB.Serialization;

namespace LeaderBoard.SharedKernel.EventStoreDB.Events;

public static class EventEnvelopeExtensions
{
    public static IEventEnvelope? ToEventEnvelope(this ResolvedEvent resolvedEvent)
    {
        var eventData = resolvedEvent.Deserialize();
        var eventMetadata = resolvedEvent.DeserializePropagationContext();

        if (eventData == null)
            return null;

        var metaData = new EventMetadata(
            resolvedEvent.Event.EventId.ToString(),
            resolvedEvent.Event.EventNumber.ToUInt64(),
            resolvedEvent.Event.Position.CommitPosition,
            eventMetadata
        );

        return EventEnvelopeFactory.From(eventData, metaData);
    }
}
