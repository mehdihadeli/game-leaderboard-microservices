using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.SharedKernel.Contracts.Data.EventStore;
using StackExchange.Redis;

namespace LeaderBoard.ReadThrough.Providers;

public class EventStoreDbReadProviderDatabase : IReadProviderDatabase
{
    private readonly IAggregateStore _aggregateStore;
    private readonly IDatabase _redisDatabase;

    public EventStoreDbReadProviderDatabase(
        IAggregateStore aggregateStore,
        IConnectionMultiplexer redisConnection
    )
    {
        _aggregateStore = aggregateStore;
        _redisDatabase = redisConnection.GetDatabase();
    }

    public IQueryable<PlayerScoreReadModel> GetScoresAndRanks(
        string leaderBoardName,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public async Task<PlayerScoreReadModel?> GetGlobalScoreAndRank(
        string leaderBoardName,
        string playerId,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    )
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

        return null;
    }
}
