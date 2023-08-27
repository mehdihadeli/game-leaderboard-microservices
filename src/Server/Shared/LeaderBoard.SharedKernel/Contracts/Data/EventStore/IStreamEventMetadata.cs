using OpenTelemetry.Context.Propagation;

namespace LeaderBoard.SharedKernel.Contracts.Data.EventStore;

public interface IStreamEventMetadata
{
    string EventId { get; }
	ulong? LogPosition { get; }
	ulong StreamPosition { get; }
	PropagationContext? PropagationContext { get; }
}
