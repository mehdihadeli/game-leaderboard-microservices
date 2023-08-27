using EventStore.Client;
using LeaderBoard.SharedKernel.Contracts.Data.EventStore;
using LeaderBoard.SharedKernel.Core.Data.EventStore;
using LeaderBoard.SharedKernel.EventStoreDB.Serialization;

namespace LeaderBoard.SharedKernel.EventStoreDB.Events;

public static class StreamEventExtensions
{
    public static IStreamEvent? ToStreamEvent(this ResolvedEvent resolvedEvent)
    {
        var eventData = resolvedEvent.Deserialize();
        var eventMetadata = resolvedEvent.DeserializePropagationContext();

        if (eventData == null)
            return null;

        var metaData = new StreamEventMetadata(
            resolvedEvent.Event.EventId.ToString(),
            resolvedEvent.Event.EventNumber.ToUInt64(),
            resolvedEvent.Event.Position.CommitPosition,
            eventMetadata
        );

        return StreamEventFactory.From(eventData, metaData);
    }
}
