using LeaderBoard.SharedKernel.Application.Data.EFContext;
using LeaderBoard.SharedKernel.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaderBoard.ReadThrough.Providers;

public class PostgresReadProviderDatabase : IReadProviderDatabase
{
    private readonly LeaderBoardDBContext _leaderBoardDbContext;

    public PostgresReadProviderDatabase(LeaderBoardDBContext leaderBoardDbContext)
    {
        _leaderBoardDbContext = leaderBoardDbContext;
    }

    public IQueryable<PlayerScore> GetScoresAndRanks(
        string leaderBoardName,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<PlayerScore> postgresItems = isDesc
            ? _leaderBoardDbContext.PlayerScores
                .AsNoTracking()
                .Where(x => x.LeaderBoardName == Constants.GlobalLeaderBoard)
                .OrderByDescending(x => x.Score)
            : _leaderBoardDbContext.PlayerScores
                .AsNoTracking()
                .Where(x => x.LeaderBoardName == Constants.GlobalLeaderBoard)
                .OrderBy(x => x.Score);

        return postgresItems;
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
