using LeaderBoard.WriteBehind.Shared.Dtos;

namespace LeaderBoard.WriteBehind.Shared.DatabaseProviders;

public interface IWriteBehindDatabaseProvider
{
    Task AddOrUpdatePlayerScore(PlayerScoreDto playerScoreDto, CancellationToken cancellationToken);
}
