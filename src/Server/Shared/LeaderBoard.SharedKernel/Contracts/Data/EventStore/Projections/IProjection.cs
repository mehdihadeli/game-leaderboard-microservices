namespace LeaderBoard.SharedKernel.Contracts.Data.EventStore.Projections;

public interface IProjection
{
    void When(object @event);
}

public interface IVersionedProjection: IProjection
{
    public ulong LastProcessedPosition { get; set; }
}
