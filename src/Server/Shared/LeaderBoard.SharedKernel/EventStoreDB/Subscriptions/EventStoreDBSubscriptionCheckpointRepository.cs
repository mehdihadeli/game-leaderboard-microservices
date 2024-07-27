using EventStore.Client;
using LeaderBoard.SharedKernel.EventStoreDB.Serialization;

namespace LeaderBoard.SharedKernel.EventStoreDB.Subscriptions;

// Ref: https://github.com/oskardudycz/EventSourcing.NetCore/tree/main/Sample

public record CheckpointStored(string SubscriptionId, ulong? Position, DateTime CheckpointedAt);

public class EventStoreDBSubscriptionCheckpointRepository : ISubscriptionCheckpointRepository
{
    private readonly EventStoreClient _eventStoreClient;

    public EventStoreDBSubscriptionCheckpointRepository(EventStoreClient eventStoreClient)
    {
        this._eventStoreClient = eventStoreClient ?? throw new ArgumentNullException(nameof(eventStoreClient));
    }

    public async ValueTask<ulong?> Load(string subscriptionId, CancellationToken ct)
    {
        var streamName = GetCheckpointStreamName(subscriptionId);

        var result = _eventStoreClient.ReadStreamAsync(
            Direction.Backwards,
            streamName,
            StreamPosition.End,
            1,
            cancellationToken: ct
        );

        if (await result.ReadState.ConfigureAwait(false) == ReadState.StreamNotFound)
        {
            return null;
        }

        ResolvedEvent? @event = await result.FirstOrDefaultAsync(ct).ConfigureAwait(false);

        return @event?.Deserialize<CheckpointStored>()?.Position;
    }

    public async ValueTask Store(string subscriptionId, ulong position, CancellationToken ct)
    {
        var @event = new CheckpointStored(subscriptionId, position, DateTime.UtcNow);
        var eventToAppend = new[] { @event.ToJsonEventData() };
        var streamName = GetCheckpointStreamName(subscriptionId);

        try
        {
            // store new checkpoint expecting stream to exist
            await _eventStoreClient
                .AppendToStreamAsync(streamName, StreamState.StreamExists, eventToAppend, cancellationToken: ct)
                .ConfigureAwait(false);
        }
        catch (WrongExpectedVersionException)
        {
            // WrongExpectedVersionException means that stream did not exist
            // Set the checkpoint stream to have at most 1 event
            // using stream metadata $maxCount property
            await _eventStoreClient
                .SetStreamMetadataAsync(streamName, StreamState.NoStream, new StreamMetadata(1), cancellationToken: ct)
                .ConfigureAwait(false);

            // append event again expecting stream to not exist
            await _eventStoreClient
                .AppendToStreamAsync(streamName, StreamState.NoStream, eventToAppend, cancellationToken: ct)
                .ConfigureAwait(false);
        }
    }

    private static string GetCheckpointStreamName(string subscriptionId) => $"checkpoint_{subscriptionId}";
}
