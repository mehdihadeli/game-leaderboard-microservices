using LeaderBoard.SharedKernel.Application.Messages.PlayerScore;
using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.SharedKernel.Redis;
using LeaderBoard.WriteBehind.Providers;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LeaderBoard.WriteBehind;

// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services
public class WriteBehindWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WriteBehindWorker> _logger;
    private readonly WriteBehindOptions _options;

    public WriteBehindWorker(
        IServiceProvider serviceProvider,
        ILogger<WriteBehindWorker> logger,
        IOptions<WriteBehindOptions> options
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var redis = _serviceProvider.GetRequiredService<IConnectionMultiplexer>();
        var redisDatabase = redis.GetDatabase();

        using var scope = _serviceProvider.CreateScope();

        if (_options.UseRedisPubSubWriteBehind)
        {
            await redisDatabase.SubscribeMessage<PlayerScoreAdded>(
                (chanName, message) =>
                {
                    _logger.LogInformation(
                        "{PlayerScoreAdded} message received from redis pub/sub",
                        message
                    );

                    var writeBehindDatabaseProviders = scope.ServiceProvider.GetRequiredService<
                        IEnumerable<IWriteBehindDatabaseProvider>
                    >();
                    foreach (var writeBehindProvider in writeBehindDatabaseProviders)
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

            await redisDatabase.SubscribeMessage<PlayerScoreUpdated>(
                (chanName, message) =>
                {
                    _logger.LogInformation(
                        "{PlayerScoreUpdated} message received from redis pub/sub",
                        message
                    );
                    var writeBehindDatabaseProviders = scope.ServiceProvider.GetRequiredService<
                        IEnumerable<IWriteBehindDatabaseProvider>
                    >();
                    foreach (var writeBehindProvider in writeBehindDatabaseProviders)
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

        while (!stoppingToken.IsCancellationRequested && _options.UseRedisStreamWriteBehind)
        {
            var sp = scope.ServiceProvider;
            var writeBehind = sp.GetRequiredService<IRedisStreamWriteBehind>();
            await writeBehind.Execute(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        var redis = _serviceProvider.GetRequiredService<IConnectionMultiplexer>();
        await redis.GetSubscriber().UnsubscribeAllAsync();
        await base.StopAsync(cancellationToken);
    }
}
