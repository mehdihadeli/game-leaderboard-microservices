﻿namespace LeaderBoard.SharedKernel.EventStoreDB.Subscriptions;

public interface ISubscriptionCheckpointRepository
{
    ValueTask<ulong?> Load(string subscriptionId, CancellationToken ct);

    ValueTask Store(string subscriptionId, ulong position, CancellationToken ct);
}
