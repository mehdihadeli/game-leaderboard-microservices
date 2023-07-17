using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.SharedKernel.Contracts.Data.EventStore;
using LeaderBoard.WriteThrough.PlayerScore.Dtos;

namespace LeaderBoard.WriteThrough.Shared.Providers;

public class EventStoreWriteProviderDatabase : IWriteProviderDatabase
{
    private readonly IAggregateStore _aggregateStore;
    private readonly ILogger<EventStoreWriteProviderDatabase> _logger;

    public EventStoreWriteProviderDatabase(
        IAggregateStore aggregateStore,
        ILogger<EventStoreWriteProviderDatabase> logger
    )
    {
        _aggregateStore = aggregateStore;
        _logger = logger;
    }

    public async Task AddOrUpdatePlayerScore(
        PlayerScoreDto playerScore,
        CancellationToken cancellationToken
    )
    {
        // write to primary database
        var playerScoreAggregate = await _aggregateStore.GetAsync<PlayerScoreAggregate, string>(
            playerScore.PlayerId,
            cancellationToken
        );

        if (playerScoreAggregate is null)
        {
            // create a new aggregate
            playerScoreAggregate = PlayerScoreAggregate.Create(
                playerScore.PlayerId,
                playerScore.Score,
                playerScore.LeaderBoardName,
                playerScore.FirstName,
                playerScore.LeaderBoardName,
                playerScore.Country
            );
        }
        else
        {
            // update existing aggregate
            playerScoreAggregate.Update(
                playerScore.Score,
                playerScore.FirstName,
                playerScore.LastName,
                playerScore.Country
            );
        }

        var appendResult = await _aggregateStore.StoreAsync<PlayerScoreAggregate, string>(
            playerScoreAggregate,
            cancellationToken
        );
    }
}
