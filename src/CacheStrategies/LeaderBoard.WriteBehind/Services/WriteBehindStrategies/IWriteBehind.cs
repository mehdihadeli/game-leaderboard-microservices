namespace LeaderBoard.WriteBehind.Services.WriteBehindStrategies;

public interface IWriteBehind
{
	Task Execute(CancellationToken cancellationToken);
}