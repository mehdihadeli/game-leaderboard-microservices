using LeaderBoard.SharedKernel.Application.Data.EFContext;
using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.SharedKernel.Contracts.Data.EventStore;
using Microsoft.EntityFrameworkCore;

namespace LeaderBoard.ReadThrough.Providers;

public class PostgresReadProviderDatabase : IReadProviderDatabase
{
    private readonly LeaderBoardReadDbContext _leaderBoardReadDbContext;
    private readonly IAggregateStore _aggregateStore;

    public PostgresReadProviderDatabase(
        LeaderBoardReadDbContext leaderBoardReadDbContext,
        IAggregateStore aggregateStore
    )
    {
        _leaderBoardReadDbContext = leaderBoardReadDbContext;
        _aggregateStore = aggregateStore;
    }

    public IQueryable<PlayerScoreReadModel> GetScoresAndRanks(
        string leaderBoardName,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<PlayerScoreReadModel> postgresItems = isDesc
            ? _leaderBoardReadDbContext.PlayerScores
                .AsNoTracking()
                .Where(x => x.LeaderBoardName == Constants.GlobalLeaderBoard)
                .OrderByDescending(x => x.Score)
            : _leaderBoardReadDbContext.PlayerScores
                .AsNoTracking()
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
        // first check Ef postgres database and if data doesn't exists we go to EventStoreDb database
        var data = await _leaderBoardReadDbContext.PlayerScores.SingleOrDefaultAsync(
            x => x.PlayerId == playerId && x.LeaderBoardName == leaderBoardName,
            cancellationToken: cancellationToken
        );

        if (data is null)
        {
            var playerScoreAggregate = await _aggregateStore.GetAsync<PlayerScoreAggregate, string>(
                playerId,
                cancellationToken
            );
            if (playerScoreAggregate != null)
            {
                return new PlayerScoreReadModel
                {
                    Score = playerScoreAggregate.Score,
                    PlayerId = playerId,
                    LeaderBoardName = leaderBoardName,
                    FirstName = playerScoreAggregate.FirstName,
                    LastName = playerScoreAggregate.LastName,
                    Country = playerScoreAggregate.Country,
                    CreatedAt = playerScoreAggregate.Created
                };
            }
        }

        return data;
    }
}
