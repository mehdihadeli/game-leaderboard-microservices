using LeaderBoard.SharedKernel.Application.Messages.PlayerScore;
using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.WriteBehind.Providers;
using MassTransit;

namespace LeaderBoard.WriteBehind.Consumers;

public class PlayerScoreAddedConsumer : IConsumer<PlayerScoreAdded>
{
    private readonly ILogger<PlayerScoreAddedConsumer> _logger;
    private readonly IEnumerable<IWriteBehindDatabaseProvider> _writeBehindDatabaseProviders;

    public PlayerScoreAddedConsumer(
        ILogger<PlayerScoreAddedConsumer> logger,
        IEnumerable<IWriteBehindDatabaseProvider> writeBehindDatabaseProviders
    )
    {
        _logger = logger;
        _writeBehindDatabaseProviders = writeBehindDatabaseProviders;
    }

    public async Task Consume(ConsumeContext<PlayerScoreAdded> context)
    {
        _logger.LogInformation("PlayerScoreAdded message received");

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
