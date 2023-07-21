namespace LeaderBoard.SharedKernel.Contracts.Data.EventStore;

public interface IStreamEventMetadata
{
    string EventId { get; }
    long? LogPosition { get; }
    long StreamPosition { get; }
}
