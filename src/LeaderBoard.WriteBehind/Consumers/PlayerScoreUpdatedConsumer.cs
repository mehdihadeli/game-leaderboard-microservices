using LeaderBoard.MessageContracts.PlayerScore;
using LeaderBoard.WriteBehind.Providers;
using MassTransit;

namespace LeaderBoard.WriteBehind.Consumers;

public class PlayerScoreUpdatedConsumer : IConsumer<PlayerScoreUpdated>
{
    readonly ILogger<PlayerScoreUpdatedConsumer> _logger;
    private readonly IEnumerable<IWriteBehindDatabaseProvider> _writeBehindDatabaseProviders;

    public PlayerScoreUpdatedConsumer(
        ILogger<PlayerScoreUpdatedConsumer> logger,
        IEnumerable<IWriteBehindDatabaseProvider> writeBehindDatabaseProviders
    )
    {
        _logger = logger;
        _writeBehindDatabaseProviders = writeBehindDatabaseProviders;
    }

    public async Task Consume(ConsumeContext<PlayerScoreUpdated> context)
    {
        _logger.LogInformation("PlayerScoreUpdated message received");

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
