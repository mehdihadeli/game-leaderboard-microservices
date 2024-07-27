using Humanizer;
using LeaderBoard.SharedKernel.Application.Data.EFContext;
using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.SharedKernel.Contracts.Data;
using LeaderBoard.SharedKernel.Contracts.Data.EventStore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LeaderBoard.GameEventsProcessor.Shared.Data;

public class DataSeeder : ISeeder
{
    private readonly IConnectionMultiplexer _redisConnection;
    private readonly LeaderBoardReadDbContext _leaderBoardReadDbContext;
    private readonly IAggregateStore _aggregateStore;
    private readonly LeaderBoardOptions _leaderBoardOptions;
    private readonly IDatabase _redisDatabase;

    public DataSeeder(
        IConnectionMultiplexer redisConnection,
        LeaderBoardReadDbContext leaderBoardReadDbContext,
        IOptions<LeaderBoardOptions> leaderBoardOptions,
        IAggregateStore aggregateStore
    )
    {
        _redisConnection = redisConnection;
        _leaderBoardReadDbContext = leaderBoardReadDbContext;
        _aggregateStore = aggregateStore;
        _leaderBoardOptions = leaderBoardOptions.Value;
        _redisDatabase = redisConnection.GetDatabase();
    }

    public async Task SeedAsync()
    {
        if (_leaderBoardOptions.CleanupRedisOnStart)
        {
            await DeleteAllKeys();
        }

        // Because we delete all caches in each run we fill our cache again from our primary database
        if (_leaderBoardOptions.UseCacheWarmUp)
        {
            await WarmingUpCache();
        }
    }

    private async Task WarmingUpCache()
    {
        var sortedsetLenght = _redisDatabase.SortedSetLength(Constants.GlobalLeaderBoard);
        if (sortedsetLenght == 0)
        {
            bool isDesc = true;

            // loading all data from EF postgres database to cache
            // for working ranks correctly we should load all items form primary database to cache for using sortedset for calculating ranks
            IQueryable<PlayerScoreReadModel> postgresItems = isDesc
                ? _leaderBoardReadDbContext
                    .PlayerScores.AsNoTracking()
                    .Where(x => x.LeaderBoardName == Constants.GlobalLeaderBoard)
                    .OrderByDescending(x => x.Score)
                : _leaderBoardReadDbContext
                    .PlayerScores.AsNoTracking()
                    .Where(x => x.LeaderBoardName == Constants.GlobalLeaderBoard)
                    .OrderBy(x => x.Score);

            await PopulateCache(postgresItems);
        }
    }

    private async Task PopulateCache(IQueryable<PlayerScoreReadModel> databaseQuery)
    {
        await foreach (var playerScore in LoadDatabaseItemByPaging(databaseQuery))
        {
            await PopulateCache(playerScore);
        }
    }

    //https://dev.to/mbernard/asynchronous-streams-in-c-8-0-33la
    private async IAsyncEnumerable<PlayerScoreReadModel> LoadDatabaseItemByPaging(
        IQueryable<PlayerScoreReadModel> databaseQuery
    )
    {
        int pageNumber = 0;
        int pageSize = 1000;
        bool hasMoreData = true;

        while (hasMoreData)
        {
            var data = await databaseQuery.Skip(pageNumber * pageSize).Take(pageSize).ToListAsync();

            foreach (var entity in data)
            {
                yield return entity;
            }

            hasMoreData = data.Count == pageSize;
            pageNumber++;
        }
    }

    private async Task PopulateCache(PlayerScoreReadModel playerScore)
    {
        var key = $"{nameof(PlayerScoreReadModel).Underscore()}:{playerScore.PlayerId}";

        // store the summary of our player-score in sortedset, it cannot store all information
        await _redisDatabase.SortedSetAddAsync(playerScore.LeaderBoardName, key, playerScore.Score);

        // store detail of out score-player in hashset. it is related to its score information with their same unique identifier
        await _redisDatabase.HashSetAsync(
            key,
            new HashEntry[]
            {
                new(nameof(PlayerScoreReadModel.Country).Underscore(), playerScore.Country),
                new(nameof(PlayerScoreReadModel.FirstName).Underscore(), playerScore.FirstName),
                new(nameof(PlayerScoreReadModel.LastName).Underscore(), playerScore.LastName)
            }
        );
    }

    private async Task DeleteAllKeys()
    {
        // https://github.com/StackExchange/StackExchange.Redis/blob/main/src/StackExchange.Redis/RedisDatabase.cs
        foreach (var endPoint in _redisConnection.GetEndPoints())
        {
            var server = _redisConnection.GetServer(endPoint);
            var keys = server.Keys().ToList();

            if (keys.Count != 0)
            {
                var keyArr = keys.ToArray();

                try
                {
                    var query = keyArr.GroupBy(x => x.GetHashCode());

                    foreach (IGrouping<int, RedisKey> keyGroup in query)
                    {
                        foreach (var key in keyGroup)
                        {
                            await _redisDatabase.KeyDeleteAsync(key);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // ignored
                }
            }
        }
    }
}
