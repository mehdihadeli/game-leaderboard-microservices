using LeaderBoard.SharedKernel.Application.Messages.PlayerScore;
using LeaderBoard.WriteBehind.Shared.DatabaseProviders;
using LeaderBoard.WriteBehind.Shared.Dtos;
using MassTransit;
using Microsoft.Extensions.Options;

namespace LeaderBoard.WriteBehind.Shared.Services.WriteBehindStrategies.Broker.Consumers;

public class PlayerScoreAddOrUpdatedConsumer : IConsumer<PlayerScoreAddOrUpdated>
{
    private readonly ILogger<PlayerScoreAddOrUpdatedConsumer> _logger;
    private readonly IWriteBehindDatabaseProvider _eventStoreDbDatabaseProvider;
    private readonly WriteBehindOptions _writeBehindOptions;

    public PlayerScoreAddOrUpdatedConsumer(
        ILogger<PlayerScoreAddOrUpdatedConsumer> logger,
        IOptions<WriteBehindOptions> writeBehindOptions,
        IWriteBehindDatabaseProvider eventStoreDbDatabaseProvider
    )
    {
        _logger = logger;
        _eventStoreDbDatabaseProvider = eventStoreDbDatabaseProvider;
        _writeBehindOptions = writeBehindOptions.Value;
    }

    public async Task Consume(ConsumeContext<PlayerScoreAddOrUpdated> context)
    {
        _logger.LogInformation("PlayerScoreAdded message received from rabbitmq server");
        if (!_writeBehindOptions.UseBrokerWriteBehind)
        {
            _logger.LogInformation(
                "Message {Message} dropped from rabbitmq server",
                context.Message
            );
            return;
        }

        var message = context.Message;

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
}
