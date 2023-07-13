using Humanizer;
using LeaderBoard.MessageContracts.PlayerScore;
using LeaderBoard.WriteBehind.Models;
using LeaderBoard.WriteBehind.Providers;
using StackExchange.Redis;

namespace LeaderBoard.WriteBehind;

public class RedisStreamWriteBehind : IRedisStreamWriteBehind
{
    private readonly IConnectionMultiplexer _redisConnection;
    private readonly IEnumerable<IWriteBehindDatabaseProvider> _writeBehindDatabaseProviders;
    private readonly IDatabase _redisDatabase;

    public RedisStreamWriteBehind(
        IConnectionMultiplexer redisConnection,
        IEnumerable<IWriteBehindDatabaseProvider> writeBehindDatabaseProviders
    )
    {
        _redisConnection = redisConnection;
        _writeBehindDatabaseProviders = writeBehindDatabaseProviders;
        _redisDatabase = redisConnection.GetDatabase();
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        await HandlePlayerScoreAdded(cancellationToken);
        await HandlePlayerScoreUpdated(cancellationToken);
    }

    private async Task HandlePlayerScoreUpdated(CancellationToken cancellationToken)
    {
        var prefix = GetStreamNamePrefix<PlayerScoreUpdated>();
        IEnumerable<RedisKey> streamKeys = GetStreamKeysByPattern(prefix, _redisConnection);

        foreach (var streamKey in streamKeys)
        {
            //Get the last entry
            StreamEntry[] results = _redisDatabase.StreamRange(
                streamKey,
                maxId: "+",
                count: 1,
                messageOrder: Order.Descending
            );
            NameValueEntry[] values = results[0].Values;

            string? playerId = values
                .SingleOrDefault(x => x.Name == nameof(PlayerScore.PlayerId).Underscore())
                .Value;
            string? leaderboardName = values
                .SingleOrDefault(x => x.Name == nameof(PlayerScore.LeaderBoardName).Underscore())
                .Value;
            long? rank = (long?)
                values.SingleOrDefault(x => x.Name == nameof(PlayerScore.Rank).Underscore()).Value;
            double? score = (double?)
                values.SingleOrDefault(x => x.Name == nameof(PlayerScore.Score).Underscore()).Value;

            foreach (var writeBehindProvider in _writeBehindDatabaseProviders)
            {
                await writeBehindProvider.UpdatePlayerScore(
                    leaderboardName!,
                    playerId!,
                    score ?? 0,
                    rank,
                    cancellationToken
                );

                // remove stream-key from redis after persist on primary database
                _redisDatabase.KeyExpire(streamKey, TimeSpan.FromSeconds(5));
            }
        }
    }

    private async Task HandlePlayerScoreAdded(CancellationToken cancellationToken)
    {
        var prefix = GetStreamNamePrefix<PlayerScoreAdded>();
        IEnumerable<RedisKey> streamKeys = GetStreamKeysByPattern(prefix, _redisConnection);

        foreach (var streamKey in streamKeys)
        {
            //Get the last entry
            StreamEntry[] results = _redisDatabase.StreamRange(
                streamKey,
                maxId: "+",
                count: 1,
                messageOrder: Order.Descending
            );
            NameValueEntry[] values = results[0].Values;

            string? playerId = values
                .SingleOrDefault(x => x.Name == nameof(PlayerScore.PlayerId).Underscore())
                .Value;
            string? leaderboardName = values
                .SingleOrDefault(x => x.Name == nameof(PlayerScore.LeaderBoardName).Underscore())
                .Value;
            string? firstName = values
                .SingleOrDefault(x => x.Name == nameof(PlayerScore.FirstName).Underscore())
                .Value;
            string? lastName = values
                .SingleOrDefault(x => x.Name == nameof(PlayerScore.LastName).Underscore())
                .Value;
            string? country = values
                .SingleOrDefault(x => x.Name == nameof(PlayerScore.Country).Underscore())
                .Value;
            long? rank = (long?)
                values.SingleOrDefault(x => x.Name == nameof(PlayerScore.Rank).Underscore()).Value;
            double? score = (double?)
                values.SingleOrDefault(x => x.Name == nameof(PlayerScore.Score).Underscore()).Value;

            foreach (var writeBehindProvider in _writeBehindDatabaseProviders)
            {
                await writeBehindProvider.AddPlayerScore(
                    new PlayerScore
                    {
                        LeaderBoardName = leaderboardName ?? Constants.GlobalLeaderBoard,
                        PlayerId = playerId!,
                        FirstName = firstName,
                        LastName = lastName,
                        Country = country,
                        Score = score ?? 0,
                        Rank = rank
                    },
                    cancellationToken
                );

                // remove stream-key from redis after persist on primary database
                _redisDatabase.KeyExpire(streamKey, TimeSpan.FromSeconds(5));
            }
        }
    }

    private string GetStreamNamePrefix(string messageType)
    {
        return $"_{messageType}-stream-*";
    }

    private string GetStreamNamePrefix<T>()
    {
        return $"_{typeof(T).Name.Underscore()}-stream-*";
    }

    private static IEnumerable<RedisKey> GetStreamKeysByPattern(
        string pattern,
        IConnectionMultiplexer redisConnection
    )
    {
        var server = redisConnection.GetServer(redisConnection.GetEndPoints().Single());
        var keys = server.Keys(pattern: pattern);

        return keys;
    }
}
