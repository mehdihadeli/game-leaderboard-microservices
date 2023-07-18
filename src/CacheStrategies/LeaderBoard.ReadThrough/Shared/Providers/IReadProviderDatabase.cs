using LeaderBoard.SharedKernel.Application.Models;

namespace LeaderBoard.ReadThrough.Shared.Providers;

public interface IReadProviderDatabase
{
    IQueryable<PlayerScoreReadModel> GetScoresAndRanks(
        string leaderBoardName,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    );

    Task<PlayerScoreReadModel?> GetGlobalScoreAndRank(
        string leaderBoardName,
        string playerId,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    );
}
