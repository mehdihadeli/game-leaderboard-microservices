using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.SharedKernel.Contracts.Data.EventStore;
using LeaderBoard.WriteBehind.Dtos;

namespace LeaderBoard.WriteBehind.DatabaseProviders;

public class EventStoreDbWriteBehindDatabaseProvider : IWriteBehindDatabaseProvider
{
    private readonly IAggregateStore _aggregateStore;
    private readonly ILogger<EventStoreDbWriteBehindDatabaseProvider> _logger;

    public EventStoreDbWriteBehindDatabaseProvider(
        IAggregateStore aggregateStore,
        ILogger<EventStoreDbWriteBehindDatabaseProvider> logger
    )
    {
        _aggregateStore = aggregateStore;
        _logger = logger;
    }

    public async Task AddOrUpdatePlayerScore(
        PlayerScoreDto playerScoreDto,
        CancellationToken cancellationToken
    )
    {
        // write to primary database
        var playerScoreAggregate = await _aggregateStore.GetAsync<PlayerScoreAggregate, string>(
            playerScoreDto.PlayerId,
            cancellationToken
        );

        if (playerScoreAggregate is null)
        {
            // create a new aggregate
            playerScoreAggregate = PlayerScoreAggregate.Create(
                playerScoreDto.PlayerId,
                playerScoreDto.Score,
                playerScoreDto.LeaderBoardName,
                playerScoreDto.FirstName,
                playerScoreDto.LeaderBoardName,
                playerScoreDto.Country
            );
        }
        else
        {
            // update existing aggregate
            playerScoreAggregate.Update(
                playerScoreDto.Score,
                playerScoreDto.FirstName,
                playerScoreDto.LastName,
                playerScoreDto.Country
            );
        }

        var appendResult = await _aggregateStore.StoreAsync<PlayerScoreAggregate, string>(
            playerScoreAggregate,
            cancellationToken
        );
    }
}
