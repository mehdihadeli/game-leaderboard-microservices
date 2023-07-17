using LeaderBoard.SharedKernel.Application.Messages.PlayerScore;
using LeaderBoard.SharedKernel.Redis;
using LeaderBoard.WriteBehind.DatabaseProviders;
using LeaderBoard.WriteBehind.Dtos;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LeaderBoard.WriteBehind.Services.WriteBehindStrategies.RedisPubSub;

public class RedisPubSubWriteBehind : IWriteBehind
{
    private readonly IWriteBehindDatabaseProvider _eventStoreDbDatabaseProvider;
    private readonly ILogger<RedisPubSubWriteBehind> _logger;
    private readonly WriteBehindOptions _options;
    private readonly IDatabase _redisDatabase;

    public RedisPubSubWriteBehind(
        IOptions<WriteBehindOptions> options,
        IConnectionMultiplexer readConnection,
        IWriteBehindDatabaseProvider eventStoreDbDatabaseProvider,
        ILogger<RedisPubSubWriteBehind> logger
    )
    {
        _eventStoreDbDatabaseProvider = eventStoreDbDatabaseProvider;
        _logger = logger;
        _options = options.Value;
        _redisDatabase = readConnection.GetDatabase();
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        await _redisDatabase.SubscribeMessage<PlayerScoreAddOrUpdated>(
            async (chanName, message) =>
            {
                _logger.LogInformation(
                    "{PlayerScoreAddOrUpdated} message received from redis pub/sub",
                    message
                );

                if (!_options.UseRedisPubSubWriteBehind)
                {
                    _logger.LogInformation("Message {Message} dropped from redis pub/sub", message);
                    return;
                }

                var playerScoreDto = new PlayerScoreDto(
                    message.PlayerId,
                    message.Score,
                    message.LeaderBoardName,
                    message.FirstName,
                    message.LastName,
                    message.Country
                );

                await _eventStoreDbDatabaseProvider.AddOrUpdatePlayerScore(playerScoreDto, default);
            }
        );
    }
}
