using LeaderBoard.SharedKernel.Contracts.Data.EventStore;
using OpenTelemetry.Context.Propagation;

namespace LeaderBoard.SharedKernel.Core.Data.EventStore;

public record StreamEventMetadata(
    string EventId,
    ulong StreamPosition,
    ulong? LogPosition,
    PropagationContext? PropagationContext
) : IStreamEventMetadata;
