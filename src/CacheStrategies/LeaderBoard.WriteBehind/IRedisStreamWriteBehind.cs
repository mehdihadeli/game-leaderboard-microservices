namespace LeaderBoard.WriteBehind;

public interface IRedisStreamWriteBehind
{
	Task Execute(CancellationToken cancellationToken);
}