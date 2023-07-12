using LeaderBoard.ReadThrough.Infrastructure.Data.EFContext;
using LeaderBoard.ReadThrough.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaderBoard.ReadThrough.Providers;

public class PostgresReadProviderStore : IReadProviderStore
{
    private readonly LeaderBoardDBContext _leaderBoardDbContext;

    public PostgresReadProviderStore(LeaderBoardDBContext leaderBoardDbContext)
    {
        _leaderBoardDbContext = leaderBoardDbContext;
    }

    public IQueryable<PlayerScore> GetScoresAndRanks(
        string leaderBoardName,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    )
    {
        var postgresItems = isDesc
            ? _leaderBoardDbContext.PlayerScores.AsNoTracking().OrderByDescending(x => x.Score)
            : _leaderBoardDbContext.PlayerScores.AsNoTracking().OrderBy(x => x.Score);

        return postgresItems.Where(x => x.LeaderBoardName == leaderBoardName);
    }

    public async Task<PlayerScore?> GetGlobalScoreAndRank(
        string leaderBoardName,
        string playerId,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    )
    {
        var data = await _leaderBoardDbContext.PlayerScores.SingleOrDefaultAsync(
            x => x.PlayerId == playerId && x.LeaderBoardName == leaderBoardName,
            cancellationToken: cancellationToken
        );

        return data;
    }
}
