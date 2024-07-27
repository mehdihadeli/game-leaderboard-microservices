using System.Collections.Immutable;
using EventStore.Client;
using LeaderBoard.SharedKernel.Contracts.Data.EventStore;
using LeaderBoard.SharedKernel.Contracts.Domain.EventSourcing;
using LeaderBoard.SharedKernel.EventStoreDB.Extensions;
using LeaderBoard.SharedKernel.EventStoreDB.Serialization;

namespace LeaderBoard.SharedKernel.EventStoreDB;

// https://developers.eventstore.com/clients/dotnet/21.2/migration-to-gRPC.html
public class EventStoreDBEventStore : IEventStore
{
    private readonly EventStoreClient _grpcClient;

    public EventStoreDBEventStore(EventStoreClient grpcClient)
    {
        _grpcClient = grpcClient;
    }

    public async Task<bool> StreamExists(string streamId, CancellationToken cancellationToken = default)
    {
        var read = _grpcClient.ReadStreamAsync(
            Direction.Forwards,
            streamId,
            StreamPosition.Start,
            1,
            cancellationToken: cancellationToken
        );

        var state = await read.ReadState;
        return state == ReadState.Ok;
    }

    public async Task<IEnumerable<IStreamEvent>> GetStreamEventsAsync(
        string streamId,
        StreamReadPosition? fromVersion = null,
        int maxCount = int.MaxValue,
        CancellationToken cancellationToken = default
    )
    {
        var readResult = _grpcClient.ReadStreamAsync(
            Direction.Forwards,
            streamId,
            fromVersion?.AsStreamPosition() ?? StreamPosition.Start,
            maxCount,
            cancellationToken: cancellationToken
        );

        var resolvedEvents = await readResult.ToListAsync(cancellationToken);

        return resolvedEvents.ToStreamEvents();
    }

    public Task<IEnumerable<IStreamEvent>> GetStreamEventsAsync(
        string streamId,
        StreamReadPosition? fromVersion = null,
        CancellationToken cancellationToken = default
    )
    {
        return GetStreamEventsAsync(streamId, fromVersion, int.MaxValue, cancellationToken);
    }

    public Task<AppendResult> AppendEventAsync(
        string streamId,
        IStreamEvent @event,
        CancellationToken cancellationToken = default
    )
    {
        return AppendEventsAsync(
            streamId,
            new List<IStreamEvent> { @event }.ToImmutableList(),
            ExpectedStreamVersion.NoStream,
            cancellationToken
        );
    }

    public Task<AppendResult> AppendEventAsync(
        string streamId,
        IStreamEvent @event,
        ExpectedStreamVersion expectedRevision,
        CancellationToken cancellationToken = default
    )
    {
        return AppendEventsAsync(
            streamId,
            new List<IStreamEvent> { @event }.ToImmutableList(),
            expectedRevision,
            cancellationToken
        );
    }

    public async Task<AppendResult> AppendEventsAsync(
        string streamId,
        IReadOnlyCollection<IStreamEvent> events,
        ExpectedStreamVersion expectedRevision,
        CancellationToken cancellationToken = default
    )
    {
        var eventsData = events.Select(x => x.ToJsonEventData());

        if (expectedRevision == ExpectedStreamVersion.NoStream)
        {
            var result = await _grpcClient.AppendToStreamAsync(
                streamId,
                StreamState.NoStream,
                eventsData,
                cancellationToken: cancellationToken
            );

            return new AppendResult(
                (long)result.LogPosition.CommitPosition,
                result.NextExpectedStreamRevision.ToInt64()
            );
        }

        if (expectedRevision == ExpectedStreamVersion.Any)
        {
            var result = await _grpcClient.AppendToStreamAsync(
                streamId,
                StreamState.Any,
                eventsData,
                cancellationToken: cancellationToken
            );

            return new AppendResult(
                (long)result.LogPosition.CommitPosition,
                result.NextExpectedStreamRevision.ToInt64()
            );
        }
        else
        {
            var result = await _grpcClient.AppendToStreamAsync(
                streamId,
                expectedRevision.AsStreamRevision(),
                eventsData,
                cancellationToken: cancellationToken
            );

            return new AppendResult(
                (long)result.LogPosition.CommitPosition,
                result.NextExpectedStreamRevision.ToInt64()
            );
        }
    }

    public async Task<TAggregate?> AggregateStreamAsync<TAggregate, TId>(
        string streamId,
        StreamReadPosition fromVersion,
        TAggregate defaultAggregateState,
        Action<object> fold,
        CancellationToken cancellationToken = default
    )
        where TAggregate : class, IEventSourcedAggregate<TId>
    {
        var readResult = _grpcClient.ReadStreamAsync(
            Direction.Forwards,
            streamId,
            fromVersion.AsStreamPosition(),
            cancellationToken: cancellationToken
        );

        if (await readResult.ReadState.ConfigureAwait(false) == ReadState.StreamNotFound)
            return null;

        // var streamEvents = (await GetStreamEventsAsync(streamId, fromVersion, int.MaxValue, cancellationToken)).Select(x => x.Data);
        return await readResult
            .Select(@event => @event.DeserializeData()!)
            .AggregateAsync(
                defaultAggregateState,
                (agg, @event) =>
                {
                    fold(@event);
                    return agg;
                },
                cancellationToken
            );
    }

    public Task<TAggregate?> AggregateStreamAsync<TAggregate, TId>(
        string streamId,
        TAggregate defaultAggregateState,
        Action<object> fold,
        CancellationToken cancellationToken = default
    )
        where TAggregate : class, IEventSourcedAggregate<TId>
    {
        return AggregateStreamAsync<TAggregate, TId>(
            streamId,
            StreamReadPosition.Start,
            defaultAggregateState,
            fold,
            cancellationToken
        );
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
