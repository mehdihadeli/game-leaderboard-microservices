using LeaderBoard.WriteThrough.PlayerScore.Dtos;

namespace LeaderBoard.WriteThrough.Shared.Providers;

public interface IWriteProviderDatabase
{
    Task AddOrUpdatePlayerScore(PlayerScoreDto playerScore, CancellationToken cancellationToken);
}
