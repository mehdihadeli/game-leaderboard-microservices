using LeaderBoard.WriteThrough.Models;

namespace LeaderBoard.WriteThrough.Providers;

public interface IWriteProviderDatabase
{
    Task AddPlayerScore(PlayerScore playerScore, CancellationToken cancellationToken = default);

    Task UpdateScore(
        string leaderBoardName,
        string playerId,
        double score,
        long? rank,
        CancellationToken cancellationToken = default
    );
}
