﻿using EventStore.Client;
using LeaderBoard.SharedKernel.Contracts.Data.EventStore.Projections;
using LeaderBoard.SharedKernel.Domain.Events;
using LeaderBoard.SharedKernel.EventStoreDB.Serialization;

namespace LeaderBoard.SharedKernel.EventStoreDB.Events;

public static class AggregateStreamExtensions
{
    public static async Task<T?> AggregateStream<T>(
        this EventStoreClient eventStore,
        Guid id,
        CancellationToken cancellationToken,
        ulong? fromVersion = null
    )
        where T : class, IProjection
    {
        var readResult = eventStore.ReadStreamAsync(
            Direction.Forwards,
            StreamNameMapper.ToStreamId<T>(id),
            fromVersion ?? StreamPosition.Start,
            cancellationToken: cancellationToken
        );

        if (await readResult.ReadState.ConfigureAwait(false) == ReadState.StreamNotFound)
            return null;

        var aggregate = (T)Activator.CreateInstance(typeof(T), true)!;

        await foreach (var @event in readResult)
        {
            var eventData = @event.Deserialize();

            aggregate.When(eventData!);
        }

        return aggregate;
    }
}
