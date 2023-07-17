using LeaderBoard.SharedKernel.Contracts.Data.EventStore;

namespace LeaderBoard.SharedKernel.Core.Data.EventStore;

public record StreamEventMetadata(string EventId, long StreamPosition) : IStreamEventMetadata
{
    public long? LogPosition { get; }
}
