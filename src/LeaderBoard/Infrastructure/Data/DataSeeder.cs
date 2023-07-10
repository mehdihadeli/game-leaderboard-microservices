using System.Globalization;
using AutoBogus;
using LeaderBoard.Infrastructure.Data.EFContext;
using LeaderBoard.Models;
using LeaderBoard.SharedKernel.Data.Contracts;
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
        await DeleteAllKeys(_redisConnection, _redisDatabase);

        var playerScores = new AutoFaker<PlayerScore>()
            .RuleFor(x => x.Country, f => f.Address.Country())
            .RuleFor(x => x.FirstName, f => f.Name.FirstName())
            .RuleFor(x => x.LastName, f => f.Name.LastName())
            .RuleFor(x => x.PlayerId, f => f.Internet.UserName())
            .RuleFor(x => x.Score, f => f.Random.Number(1, 10000))
            .RuleFor(x => x.Rank, f => null)
            .RuleFor(x => x.UpdatedAt, DateTime.Now)
            .RuleFor(x => x.CreatedAt, DateTime.Now)
            .Generate(1000);

        if (!_leaderBoardOptions.UseReadCacheAside)
        {
            foreach (var playerScore in playerScores)
            {
                // store the summary of our player-score in sortedset
                await _redisDatabase.SortedSetAddAsync(
                    Constants.GlobalLeaderBoard,
                    playerScore.PlayerId,
                    playerScore.Score
                );

                // store detail of out score-player in hashset. it is related to its score information with their same unique identifier
                await _redisDatabase.HashSetAsync(
                    playerScore.PlayerId,
                    new HashEntry[]
                    {
                        new(nameof(PlayerScore.Country).ToLower(), playerScore.Country),
                        new(nameof(PlayerScore.FirstName).ToLower(), playerScore.FirstName),
                        new(nameof(PlayerScore.LastName).ToLower(), playerScore.LastName),
                        new(
                            nameof(PlayerScore.UpdatedAt).ToLower(),
                            playerScore.UpdatedAt.ToString(CultureInfo.InvariantCulture)
                        ),
                        new(
                            nameof(PlayerScore.CreatedAt).ToLower(),
                            playerScore.CreatedAt.ToString(CultureInfo.InvariantCulture)
                        )
                    }
                );
            }
        }

        if (_leaderBoardOptions.UseReadCacheAside)
        {
            if (!_leaderBoardDbContext.PlayerScores.Any())
            {
                await _leaderBoardDbContext.AddRangeAsync(playerScores);
                await _leaderBoardDbContext.SaveChangesAsync();
            }
        }
    }

    private static async Task DeleteAllKeys(
        IConnectionMultiplexer redisConnection,
        IDatabase redisDatabase
    )
    {
        // https://github.com/StackExchange/StackExchange.Redis/blob/main/src/StackExchange.Redis/RedisDatabase.cs
        foreach (var endPoint in redisConnection.GetEndPoints())
        {
            var server = redisConnection.GetServer(endPoint);
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
                            await redisDatabase.KeyDeleteAsync(key);
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
