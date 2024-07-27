namespace LeaderBoard.WriteBehind.Shared.Services.WriteBehindStrategies;

public interface IWriteBehind
{
    Task Execute(CancellationToken cancellationToken);
}
