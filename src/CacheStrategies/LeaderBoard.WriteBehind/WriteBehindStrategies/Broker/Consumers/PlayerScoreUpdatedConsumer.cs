using LeaderBoard.SharedKernel.Application.Messages.PlayerScore;
using LeaderBoard.WriteBehind.DatabaseProviders;
using MassTransit;
using Microsoft.Extensions.Options;

namespace LeaderBoard.WriteBehind.WriteBehindStrategies.Broker.Consumers;

public class PlayerScoreUpdatedConsumer : IConsumer<PlayerScoreUpdated>
{
    readonly ILogger<PlayerScoreUpdatedConsumer> _logger;
    private readonly WriteBehindOptions _writeBehindOptions;
    private readonly IEnumerable<IWriteBehindDatabaseProvider> _writeBehindDatabaseProviders;

    public PlayerScoreUpdatedConsumer(
        ILogger<PlayerScoreUpdatedConsumer> logger,
        IOptions<WriteBehindOptions> writeBehindOptions,
        IEnumerable<IWriteBehindDatabaseProvider> writeBehindDatabaseProviders
    )
    {
        _logger = logger;
        _writeBehindOptions = writeBehindOptions.Value;
        _writeBehindDatabaseProviders = writeBehindDatabaseProviders;
    }

    public async Task Consume(ConsumeContext<PlayerScoreUpdated> context)
    {
        _logger.LogInformation("PlayerScoreUpdated message received from rabbitmq server");
        if (!_writeBehindOptions.UseBrokerWriteBehind)
        {
            _logger.LogInformation(
                "Message {Message} dropped from rabbitmq server",
                context.Message
            );
            return;
        }

        var message = context.Message;

        foreach (var writeBehindProvider in _writeBehindDatabaseProviders)
        {
            await writeBehindProvider.UpdatePlayerScore(
                message.LeaderBoardName ?? Constants.GlobalLeaderBoard,
                message.PlayerId,
                message.Score,
                message.Rank,
                default
            );
        }
    }
}
