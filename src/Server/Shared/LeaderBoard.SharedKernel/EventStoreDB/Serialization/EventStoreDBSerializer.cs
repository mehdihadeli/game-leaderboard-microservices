﻿using System.Text;
using EventStore.Client;
using LeaderBoard.SharedKernel.Domain.Events;
using LeaderBoard.SharedKernel.EventStoreDB.Events;
using Newtonsoft.Json;
using OpenTelemetry.Context.Propagation;

namespace LeaderBoard.SharedKernel.EventStoreDB.Serialization;

public static class EventStoreDBSerializer
{
    private static readonly JsonSerializerSettings SerializerSettings;

    static EventStoreDBSerializer()
    {
        SerializerSettings = new JsonSerializerSettings();

        SerializerSettings.Converters.Add(new EventStoreDBEventMetadataJsonConverter());
    }

    public static T? Deserialize<T>(this ResolvedEvent resolvedEvent)
        where T : class => Deserialize(resolvedEvent) as T;

    public static object? Deserialize(this ResolvedEvent resolvedEvent)
    {
        // get type
        var eventType = EventTypeMapper.Instance.ToType(resolvedEvent.Event.EventType);

        if (eventType == null)
            return null;

        // deserialize event
        return JsonConvert.DeserializeObject(
            Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span),
            eventType,
            SerializerSettings
        )!;
    }

    public static PropagationContext? DeserializePropagationContext(this ResolvedEvent resolvedEvent)
    {
        // get type
        var eventType = EventTypeMapper.Instance.ToType(resolvedEvent.Event.EventType);

        if (eventType == null)
            return null;

        // deserialize event
        return JsonConvert.DeserializeObject<PropagationContext>(
            Encoding.UTF8.GetString(resolvedEvent.Event.Metadata.Span),
            SerializerSettings
        )!;
    }

    public static EventData ToJsonEventData(this object @event, object? metadata = null) =>
        new(
            Uuid.NewUuid(),
            EventTypeMapper.Instance.ToName(@event.GetType()),
            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event, SerializerSettings)),
            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(metadata ?? new { }, SerializerSettings))
        );
}
