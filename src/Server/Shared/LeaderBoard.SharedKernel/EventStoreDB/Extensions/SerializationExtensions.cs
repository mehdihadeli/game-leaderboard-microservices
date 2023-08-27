using System.Text;
using EventStore.Client;
using LeaderBoard.SharedKernel.Contracts.Data.EventStore;
using LeaderBoard.SharedKernel.Contracts.Domain.Events;
using LeaderBoard.SharedKernel.Core;
using LeaderBoard.SharedKernel.Core.Data.EventStore;
using Newtonsoft.Json;

namespace LeaderBoard.SharedKernel.EventStoreDB.Extensions;

public static class SerializationExtensions
{
    public static T DeserializeData<T>(this ResolvedEvent resolvedEvent) => (T)DeserializeData(resolvedEvent);

    public static object DeserializeData(this ResolvedEvent resolvedEvent)
    {
        // get type
        var eventType = TypeMapper.GetType(resolvedEvent.Event.EventType);

        // deserialize event
        return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span), eventType)!;
    }

    public static IStreamEventMetadata DeserializeMetadata(this ResolvedEvent resolvedEvent)
    {
        // deserialize event
        return JsonConvert.DeserializeObject<StreamEventMetadata>(
            Encoding.UTF8.GetString(resolvedEvent.Event.Metadata.Span)
        )!;
    }

    public static EventData ToJsonEventData(this IStreamEvent @event)
    {
        return ToJsonEventData(@event.Data, @event.Metadata);
    }

    public static EventData ToJsonEventData(this object @event, IStreamEventMetadata? metadata = null)
    {
        return new(
            Uuid.NewUuid(),
            TypeMapper.GetTypeNameByObject(@event),
            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event)),
            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(metadata ?? new object()))
        );
    }

    public static StreamEvent? ToStreamEvent(this ResolvedEvent resolvedEvent)
    {
        var eventData = resolvedEvent.DeserializeData();
        var metaData = resolvedEvent.DeserializeMetadata();

        // var metaData = new StreamEventMetadata(
        //     resolvedEvent.Event.EventId.ToString(),
        //     resolvedEvent.Event.EventNumber.ToInt64());

        var type = typeof(StreamEvent<>).MakeGenericType(eventData.GetType());

        return (StreamEvent?)Activator.CreateInstance(type, eventData, metaData);
    }
}
