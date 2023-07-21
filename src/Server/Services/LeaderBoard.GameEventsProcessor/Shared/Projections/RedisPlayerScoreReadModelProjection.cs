using Humanizer;
using LeaderBoard.SharedKernel.Application.Events;
using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.SharedKernel.Contracts.Data.EventStore.Projections;
using LeaderBoard.SharedKernel.Contracts.Domain.Events;
using StackExchange.Redis;

namespace LeaderBoard.GameEventsProcessor.Shared.Projections;

// https://web.archive.org/web/20230128040244/https://zimarev.com/blog/event-sourcing/projections/
public class RedisPlayerScoreReadModelProjection : IReadProjection
{
    private readonly IDatabase _redisDatabase;

    public RedisPlayerScoreReadModelProjection(IConnectionMultiplexer redisConnection)
    {
        _redisDatabase = redisConnection.GetDatabase();
    }

    public async Task ProjectAsync<TEvent>(
        IEventEnvelope<TEvent> eventEnvelope,
        CancellationToken cancellationToken = default
    )
        where TEvent : IDomainEvent
    {
        var @event = eventEnvelope.Data;
        var eventMetadata = eventEnvelope.Metadata;

        switch (@event)
        {
            case PlayerScoreAdded playerScoreAdded:
                await ApplyEvent(playerScoreAdded, eventMetadata, cancellationToken);
                break;

            case PlayerScoreUpdated playerScoreUpdated:
                await ApplyEvent(playerScoreUpdated, eventMetadata, cancellationToken);
                break;
        }
    }

    private async Task ApplyEvent(
        PlayerScoreAdded @event,
        EventMetadata eventMetadata,
        CancellationToken cancellationToken
    )
    {
        await AddOrUpdate(
            @event.Id,
            @event.LeaderBoardName,
            @event.Score,
            @event.FirstName,
            @event.LastName,
            @event.Country
        );
    }

    private async Task ApplyEvent(
        PlayerScoreUpdated @event,
        EventMetadata eventMetadata,
        CancellationToken cancellationToken
    )
    {
        await AddOrUpdate(
            @event.Id,
            @event.LeaderBoardName,
            @event.Score,
            @event.FirstName,
            @event.LastName,
            @event.Country
        );
    }

    private async Task AddOrUpdate(
        string id,
        string leaderboardName,
        double score,
        string firstname,
        string lastname,
        string country
    )
    {
        var key = $"{nameof(PlayerScoreReadModel).Underscore()}:{id}";
        bool isDesc = true;
        bool exists = true;

        var currentScoreTask = _redisDatabase.SortedSetScoreAsync(leaderboardName, key);

        var currentScore = await currentScoreTask;
        if (currentScore == null)
        {
            exists = false;
        }

        // https://stackoverflow.com/questions/25976231/stackexchange-redis-transaction-methods-freezes
        // https://github.com/olsh/stack-exchange-redis-analyzer
        var transaction = _redisDatabase.CreateTransaction();

        // increment score and calculation rank in redis
        var newScoreTask = transaction.SortedSetIncrementAsync(leaderboardName, key, score);

        // because ranks will change very fast between players, storing it in primary database is useless
        var rankTask = transaction.SortedSetRankAsync(
            leaderboardName,
            key,
            isDesc ? Order.Descending : Order.Ascending
        );

        // store detail of out score-player in hashset. it is related to its score information with their same unique identifier
        var hashsetTask = transaction.HashSetAsync(
            id,
            new HashEntry[]
            {
                new(nameof(PlayerScoreReadModel.Country).Underscore(), country),
                new(nameof(PlayerScoreReadModel.FirstName).Underscore(), firstname),
                new(nameof(PlayerScoreReadModel.LastName).Underscore(), lastname),
            }
        );

        if (await transaction.ExecuteAsync())
        {
            var newScoreValue = await newScoreTask;
            var rank = await rankTask;
            rank = isDesc ? rank + 1 : rank - 1;

            await hashsetTask;
        }
    }
}