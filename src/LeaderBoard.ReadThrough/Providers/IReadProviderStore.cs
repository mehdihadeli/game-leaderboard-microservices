using LeaderBoard.ReadThrough.Models;

namespace LeaderBoard.ReadThrough.Providers;

public interface IReadProviderStore
{
    IQueryable<PlayerScore> GetScoresAndRanks(
        string leaderBoardName,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    );

    Task<PlayerScore?> GetGlobalScoreAndRank(
        string leaderBoardName,
        string playerId,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    );
}