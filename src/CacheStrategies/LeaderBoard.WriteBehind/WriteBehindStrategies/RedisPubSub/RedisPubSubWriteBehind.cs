using LeaderBoard.SharedKernel.Application.Messages.PlayerScore;
using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.SharedKernel.Redis;
using LeaderBoard.WriteBehind.DatabaseProviders;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LeaderBoard.WriteBehind.WriteBehindStrategies.RedisPubSub;

public class RedisPubSubWriteBehind : IWriteBehind
{
    private readonly IEnumerable<IWriteBehindDatabaseProvider> _writeBehindDatabaseProviders;
    private readonly ILogger<RedisPubSubWriteBehind> _logger;
    private readonly WriteBehindOptions _options;
    private readonly IDatabase _redisDatabase;

    public RedisPubSubWriteBehind(
        IOptions<WriteBehindOptions> options,
        IConnectionMultiplexer readConnection,
        IEnumerable<IWriteBehindDatabaseProvider> writeBehindDatabaseProviders,
        ILogger<RedisPubSubWriteBehind> logger
    )
    {
        _writeBehindDatabaseProviders = writeBehindDatabaseProviders;
        _logger = logger;
        _options = options.Value;
        _redisDatabase = readConnection.GetDatabase();
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        await _redisDatabase.SubscribeMessage<PlayerScoreAdded>(
            (chanName, message) =>
            {
                _logger.LogInformation(
                    "{PlayerScoreAdded} message received from redis pub/sub",
                    message
                );

                if (!_options.UseRedisPubSubWriteBehind)
                {
                    _logger.LogInformation("Message {Message} dropped from redis pub/sub", message);
                    return;
                }

                foreach (var writeBehindProvider in _writeBehindDatabaseProviders)
                {
                    writeBehindProvider.AddPlayerScore(
                        new PlayerScore
                        {
                            Score = message.Score,
                            PlayerId = message.PlayerId,
                            LeaderBoardName = message.LeaderBoardName,
                            Country = message.Country,
                            Rank = message.Rank,
                            FirstName = message.FirstName,
                            LastName = message.LastName
                        },
                        default
                    );
                }
            }
        );

        await _redisDatabase.SubscribeMessage<PlayerScoreUpdated>(
            (chanName, message) =>
            {
                _logger.LogInformation(
                    "{PlayerScoreUpdated} message received from redis pub/sub",
                    message
                );

                if (!_options.UseRedisPubSubWriteBehind)
                {
                    _logger.LogInformation("Message {Message} dropped from redis pub/sub", message);
                    return;
                }

                foreach (var writeBehindProvider in _writeBehindDatabaseProviders)
                {
                    writeBehindProvider.UpdatePlayerScore(
                        message.LeaderBoardName ?? Constants.GlobalLeaderBoard,
                        message.PlayerId,
                        message.Score,
                        message.Rank,
                        default
                    );
                }
            }
        );
    }
}
