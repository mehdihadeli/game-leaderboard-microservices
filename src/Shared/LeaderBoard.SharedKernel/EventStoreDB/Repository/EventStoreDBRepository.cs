using EventStore.Client;
using LeaderBoard.SharedKernel.Contracts.Domain;
using LeaderBoard.SharedKernel.Domain.Events;
using LeaderBoard.SharedKernel.EventStoreDB.Events;
using LeaderBoard.SharedKernel.EventStoreDB.Serialization;
using LeaderBoard.SharedKernel.OpenTelemetry;

namespace LeaderBoard.SharedKernel.EventStoreDB.Repository;

public class EventStoreDBRepository<T> : IEventStoreDBRepository<T>
    where T : class, IAggregate
{
    private readonly EventStoreClient _eventStore;
    private readonly IActivityScope _activityScope;

    public EventStoreDBRepository(EventStoreClient eventStore, IActivityScope activityScope)
    {
        _eventStore = eventStore;
        _activityScope = activityScope;
    }

    public Task<T?> Find(Guid id, CancellationToken cancellationToken) =>
        _eventStore.AggregateStream<T>(id, cancellationToken);

    public async Task<ulong> Add(T aggregate, CancellationToken ct = default)
    {
        var result = await _eventStore
            .AppendToStreamAsync(
                StreamNameMapper.ToStreamId<T>(aggregate.Id),
                StreamState.NoStream,
                GetEventsToStore(aggregate),
                cancellationToken: ct
            )
            .ConfigureAwait(false);

        return result.NextExpectedStreamRevision.ToUInt64();
    }

    public async Task<ulong> Update(
        T aggregate,
        ulong? expectedRevision = null,
        CancellationToken ct = default
    )
    {
        var eventsToAppend = GetEventsToStore(aggregate);
        var nextVersion = expectedRevision ?? (ulong)(aggregate.Version - eventsToAppend.Count);

        var result = await _eventStore
            .AppendToStreamAsync(
                StreamNameMapper.ToStreamId<T>(aggregate.Id),
                nextVersion,
                eventsToAppend,
                cancellationToken: ct
            )
            .ConfigureAwait(false);

        return result.NextExpectedStreamRevision.ToUInt64();
    }

    public Task<ulong> Delete(
        T aggregate,
        ulong? expectedRevision = null,
        CancellationToken ct = default
    ) => Update(aggregate, expectedRevision, ct);

    private static List<EventData> GetEventsToStore(T aggregate)
    {
        var events = aggregate.DequeueUncommittedEvents();

        return events
            .Select(@event => @event.ToJsonEventData(TelemetryPropagator.GetPropagationContext()))
            .ToList();
    }
}
