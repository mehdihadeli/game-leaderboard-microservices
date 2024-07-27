using Humanizer;
using LeaderBoard.SharedKernel.Application.Messages.PlayerScore;
using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.WriteBehind.Shared.DatabaseProviders;
using LeaderBoard.WriteBehind.Shared.Dtos;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LeaderBoard.WriteBehind.Shared.Services.WriteBehindStrategies.RedisStream;

public class RedisStreamWriteBehind : IWriteBehind
{
    private readonly IConnectionMultiplexer _redisConnection;
    private readonly IWriteBehindDatabaseProvider _eventStoreDbDatabaseProvider;
    private readonly ILogger<RedisStreamWriteBehind> _logger;
    private readonly WriteBehindOptions _options;
    private readonly IDatabase _redisDatabase;

    public RedisStreamWriteBehind(
        IConnectionMultiplexer redisConnection,
        IWriteBehindDatabaseProvider eventStoreDbDatabaseProvider,
        IOptions<WriteBehindOptions> options,
        ILogger<RedisStreamWriteBehind> logger
    )
    {
        _redisConnection = redisConnection;
        _eventStoreDbDatabaseProvider = eventStoreDbDatabaseProvider;
        _logger = logger;
        _options = options.Value;
        _redisDatabase = redisConnection.GetDatabase();
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await HandlePlayerScoreAddOrUpdated(cancellationToken);
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError("Error in RedisStreamWriteBehind execution: {Message}", e.Message);
            }
        }
    }

    private async Task HandlePlayerScoreAddOrUpdated(CancellationToken cancellationToken)
    {
        var prefix = GetStreamNamePrefix<PlayerScoreAddOrUpdated>();
        IEnumerable<RedisKey> streamKeys = GetStreamKeysByPattern(prefix, _redisConnection);

        foreach (var streamKey in streamKeys)
        {
            if (!_options.UseRedisStreamWriteBehind)
            {
                _logger.LogInformation("Message dropped from redis stream");
                _redisDatabase.KeyExpire(streamKey, TimeSpan.FromSeconds(1));
                return;
            }

            //Get the last entry
            StreamEntry[] results = _redisDatabase.StreamRange(
                streamKey,
                maxId: "+",
                count: 1,
                messageOrder: Order.Descending
            );
            NameValueEntry[] values = results[0].Values;

            string? playerId = values
                .SingleOrDefault(x => x.Name == nameof(PlayerScoreReadModel.PlayerId).Underscore())
                .Value;
            string? leaderboardName = values
                .SingleOrDefault(x => x.Name == nameof(PlayerScoreReadModel.LeaderBoardName).Underscore())
                .Value;
            string? firstName = values
                .SingleOrDefault(x => x.Name == nameof(PlayerScoreReadModel.FirstName).Underscore())
                .Value;
            string? lastName = values
                .SingleOrDefault(x => x.Name == nameof(PlayerScoreReadModel.LastName).Underscore())
                .Value;
            string? country = values
                .SingleOrDefault(x => x.Name == nameof(PlayerScoreReadModel.Country).Underscore())
                .Value;
            double? score = (double?)
                values.SingleOrDefault(x => x.Name == nameof(PlayerScoreReadModel.Score).Underscore()).Value;

            var playerScore = new PlayerScoreDto(
                playerId!,
                score ?? 0,
                leaderboardName ?? Constants.GlobalLeaderBoard,
                firstName ?? String.Empty,
                lastName ?? String.Empty,
                country ?? String.Empty
            );

            await _eventStoreDbDatabaseProvider.AddOrUpdatePlayerScore(playerScore, cancellationToken);

            // remove stream-key from redis after persist on primary database
            _redisDatabase.KeyExpire(streamKey, TimeSpan.FromSeconds(2));
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

    private static IEnumerable<RedisKey> GetStreamKeysByPattern(string pattern, IConnectionMultiplexer redisConnection)
    {
        var server = redisConnection.GetServer(redisConnection.GetEndPoints().Single());
        var keys = server.Keys(pattern: pattern);

        return keys;
    }
}
