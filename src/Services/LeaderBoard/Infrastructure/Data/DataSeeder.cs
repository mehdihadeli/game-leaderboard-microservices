using System.Globalization;
using AutoBogus;
using EFCore.BulkExtensions;
using Humanizer;
using LeaderBoard.Infrastructure.Data.EFContext;
using LeaderBoard.Models;
using LeaderBoard.SharedKernel.Data.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LeaderBoard.Infrastructure.Data;

public class DataSeeder : ISeeder
{
    private readonly IConnectionMultiplexer _redisConnection;
    private readonly LeaderBoardDBContext _leaderBoardDbContext;
    private readonly LeaderBoardOptions _leaderBoardOptions;
    private readonly IDatabase _redisDatabase;

    public DataSeeder(
        IConnectionMultiplexer redisConnection,
        LeaderBoardDBContext leaderBoardDbContext,
        IOptions<LeaderBoardOptions> leaderBoardOptions
    )
    {
        _redisConnection = redisConnection;
        _leaderBoardDbContext = leaderBoardDbContext;
        _leaderBoardOptions = leaderBoardOptions.Value;
        _redisDatabase = redisConnection.GetDatabase();
    }

    public async Task SeedAsync()
    {
        if (_leaderBoardOptions.CleanupRedisOnStart)
        {
            await DeleteAllKeys();
        }

        // for using cache-aside we need our database fill firstly
        // in this case our primary database is postgres and should fill before cache
        if (_leaderBoardOptions.UseReadCacheAside)
        {
            if (!_leaderBoardDbContext.PlayerScores.Any())
            {
                var playerScores = new AutoFaker<PlayerScore>()
                    .RuleFor(x => x.Country, f => f.Address.Country())
                    .RuleFor(x => x.FirstName, f => f.Name.FirstName())
                    .RuleFor(x => x.LastName, f => f.Name.LastName())
                    .RuleFor(x => x.PlayerId, f => f.Random.Guid().ToString())
                    .RuleFor(x => x.Score, f => f.Random.Number(1, 10000))
                    // we don't set rank here because for evaluating rank correctly we need all items present in database but here we have to add items one by one
                    .RuleFor(x => x.Rank, f => null)
                    .RuleFor(x => x.UpdatedAt, DateTime.Now)
                    .RuleFor(x => x.CreatedAt, DateTime.Now)
                    .RuleFor(x => x.LeaderBoardName, f => Constants.GlobalLeaderBoard)
                    .Generate(1500);

                // https://code-maze.com/dotnet-fast-inserts-entity-framework-ef-core/
                await _leaderBoardDbContext.BulkInsertAsync(playerScores);
            }

            if (_leaderBoardOptions.UseCacheWarmUp)
            {
                await WarmingUpCache();
            }
        }
    }

    private async Task WarmingUpCache()
    {
        var sortedsetLenght = _redisDatabase.SortedSetLength(Constants.GlobalLeaderBoard);
        if (sortedsetLenght == 0)
        {
            bool isDesc = true;

            // loading all data from database to cache
            // for working ranks correctly we should load all items form primary database to cache for using sortedset for calculating ranks
            IQueryable<PlayerScore> postgresItems = isDesc
                ? _leaderBoardDbContext.PlayerScores
                    .AsNoTracking()
                    .Where(x => x.LeaderBoardName == Constants.GlobalLeaderBoard)
                    .OrderByDescending(x => x.Score)
                : _leaderBoardDbContext.PlayerScores
                    .AsNoTracking()
                    .Where(x => x.LeaderBoardName == Constants.GlobalLeaderBoard)
                    .OrderBy(x => x.Score);

            await PopulateCache(postgresItems);
        }
    }

    private async Task PopulateCache(IQueryable<PlayerScore> databaseQuery)
    {
        await foreach (var playerScore in LoadDatabaseItemByPaging(databaseQuery))
        {
            await PopulateCache(playerScore);
        }
    }

    //https://dev.to/mbernard/asynchronous-streams-in-c-8-0-33la
    private async IAsyncEnumerable<PlayerScore> LoadDatabaseItemByPaging(
        IQueryable<PlayerScore> databaseQuery
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

    private async Task PopulateCache(PlayerScore playerScore)
    {
        var key = $"{nameof(PlayerScore).Underscore()}:{playerScore.PlayerId}";

        // store the summary of our player-score in sortedset, it cannot store all information
        await _redisDatabase.SortedSetAddAsync(playerScore.LeaderBoardName, key, playerScore.Score);

        // store detail of out score-player in hashset. it is related to its score information with their same unique identifier
        await _redisDatabase.HashSetAsync(
            key,
            new HashEntry[]
            {
                new(nameof(PlayerScore.Country).Underscore(), playerScore.Country),
                new(nameof(PlayerScore.FirstName).Underscore(), playerScore.FirstName),
                new(nameof(PlayerScore.LastName).Underscore(), playerScore.LastName),
                new(
                    nameof(PlayerScore.UpdatedAt).Underscore(),
                    playerScore.UpdatedAt.ToString(CultureInfo.InvariantCulture)
                ),
                new(
                    nameof(PlayerScore.CreatedAt).Underscore(),
                    playerScore.CreatedAt.ToString(CultureInfo.InvariantCulture)
                )
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
