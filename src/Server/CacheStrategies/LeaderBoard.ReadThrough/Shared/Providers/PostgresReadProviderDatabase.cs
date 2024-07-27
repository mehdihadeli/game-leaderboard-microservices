using LeaderBoard.SharedKernel.Application.Data.EFContext;
using LeaderBoard.SharedKernel.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaderBoard.ReadThrough.Shared.Providers;

public class PostgresReadProviderDatabase : IReadProviderDatabase
{
    private readonly LeaderBoardReadDbContext _leaderBoardReadDbContext;

    public PostgresReadProviderDatabase(LeaderBoardReadDbContext leaderBoardReadDbContext)
    {
        _leaderBoardReadDbContext = leaderBoardReadDbContext;
    }

    public IQueryable<PlayerScoreReadModel> GetScoresAndRanks(
        string leaderBoardName,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<PlayerScoreReadModel> postgresItems = isDesc
            ? _leaderBoardReadDbContext
                .PlayerScores.AsNoTracking()
                .Where(x => x.LeaderBoardName == Constants.GlobalLeaderBoard)
                .OrderByDescending(x => x.Score)
            : _leaderBoardReadDbContext
                .PlayerScores.AsNoTracking()
                .Where(x => x.LeaderBoardName == Constants.GlobalLeaderBoard)
                .OrderBy(x => x.Score);

        return postgresItems;
    }

    public async Task<PlayerScoreReadModel?> GetGlobalScoreAndRank(
        string leaderBoardName,
        string playerId,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    )
    {
        // https://developers.eventstore.com/server/v22.10/projections.html#streams-projection
        // First read from EF postgres database as backup database and then if not exists read from primary database EventStoreDB (but we have some limitations reading all streams and filtering!)
        var data = await _leaderBoardReadDbContext.PlayerScores.SingleOrDefaultAsync(
            x => x.PlayerId == playerId && x.LeaderBoardName == leaderBoardName,
            cancellationToken: cancellationToken
        );

        return data;
    }
}
