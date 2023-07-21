using OpenTelemetry.Context.Propagation;

namespace LeaderBoard.SharedKernel.Contracts.Domain.Events;

public record EventMetadata(
    string EventId,
    ulong StreamPosition,
    ulong LogPosition,
    PropagationContext? PropagationContext
);
