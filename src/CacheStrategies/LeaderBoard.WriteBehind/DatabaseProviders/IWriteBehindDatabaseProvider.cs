using LeaderBoard.WriteBehind.Dtos;

namespace LeaderBoard.WriteBehind.DatabaseProviders;

public interface IWriteBehindDatabaseProvider
{
    Task AddOrUpdatePlayerScore(PlayerScoreDto playerScoreDto, CancellationToken cancellationToken);
}
