using LeaderBoard.SharedKernel.Application.Models;

namespace LeaderBoard.WriteBehind.DatabaseProviders;

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
