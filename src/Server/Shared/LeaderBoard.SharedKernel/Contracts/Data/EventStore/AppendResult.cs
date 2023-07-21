namespace LeaderBoard.SharedKernel.Contracts.Data.EventStore;

public record AppendResult(long GlobalPosition, long NextExpectedVersion)
{
    public static readonly AppendResult None = new(0, -1);
};
