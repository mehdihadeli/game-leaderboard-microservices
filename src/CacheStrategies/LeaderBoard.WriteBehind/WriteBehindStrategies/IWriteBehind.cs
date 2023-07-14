namespace LeaderBoard.WriteBehind.WriteBehindStrategies;

public interface IWriteBehind
{
	Task Execute(CancellationToken cancellationToken);
}