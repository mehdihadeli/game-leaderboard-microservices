using LeaderBoard.SharedKernel.Application.Messages.PlayerScore;
using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.WriteBehind.DatabaseProviders;
using MassTransit;
using Microsoft.Extensions.Options;

namespace LeaderBoard.WriteBehind.WriteBehindStrategies.Broker.Consumers;

public class PlayerScoreAddedConsumer : IConsumer<PlayerScoreAdded>
{
    private readonly ILogger<PlayerScoreAddedConsumer> _logger;
    private readonly WriteBehindOptions _writeBehindOptions;
    private readonly IEnumerable<IWriteBehindDatabaseProvider> _writeBehindDatabaseProviders;

    public PlayerScoreAddedConsumer(
        ILogger<PlayerScoreAddedConsumer> logger,
        IOptions<WriteBehindOptions> writeBehindOptions,
        IEnumerable<IWriteBehindDatabaseProvider> writeBehindDatabaseProviders
    )
    {
        _logger = logger;
        _writeBehindOptions = writeBehindOptions.Value;
        _writeBehindDatabaseProviders = writeBehindDatabaseProviders;
    }

    public async Task Consume(ConsumeContext<PlayerScoreAdded> context)
    {
        _logger.LogInformation("PlayerScoreAdded message received from rabbitmq server");
        if (!_writeBehindOptions.UseBrokerWriteBehind)
        {
            _logger.LogInformation("Message {Message} dropped from rabbitmq server", context.Message);
            return;
        }

        var message = context.Message;

        foreach (var writeBehindProvider in _writeBehindDatabaseProviders)
        {
            await writeBehindProvider.AddPlayerScore(
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
}
