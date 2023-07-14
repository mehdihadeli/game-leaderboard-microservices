using LeaderBoard.SharedKernel.Application.Models;

namespace LeaderBoard.WriteBehind.Providers;

public interface IWriteBehindDatabaseProvider
{
    Task UpdatePlayerScore(
        string leaderBoardName,
        string playerId,
        double score,
        long? rank,
        CancellationToken cancellationToken
    );

    Task AddPlayerScore(PlayerScore playerScore, CancellationToken cancellationToken);
}
