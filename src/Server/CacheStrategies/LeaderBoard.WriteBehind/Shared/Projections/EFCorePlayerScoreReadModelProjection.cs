using LeaderBoard.SharedKernel.Application.Data.EFContext;
using LeaderBoard.SharedKernel.Application.Events;
using LeaderBoard.SharedKernel.Application.Messages;
using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.SharedKernel.Bus;
using LeaderBoard.SharedKernel.Contracts.Data.EventStore.Projections;
using LeaderBoard.SharedKernel.Contracts.Domain.Events;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;

namespace LeaderBoard.WriteBehind.Shared.Projections;

// https://web.archive.org/web/20230128040244/https://zimarev.com/blog/event-sourcing/projections/
public class EFCorePlayerScoreReadModelProjection : IReadProjection
{
    private readonly LeaderBoardReadDbContext _leaderBoardReadDbContext;
    private readonly IBusPublisher _busPublisher;
    private readonly ILogger<EFCorePlayerScoreReadModelProjection> _logger;
    private readonly IDatabase _redisDatabase;

    public EFCorePlayerScoreReadModelProjection(
        LeaderBoardReadDbContext leaderBoardReadDbContext,
        IBusPublisher busPublisher,
        IConnectionMultiplexer redisConnection,
        ILogger<EFCorePlayerScoreReadModelProjection> logger
    )
    {
        _leaderBoardReadDbContext = leaderBoardReadDbContext;
        _busPublisher = busPublisher;
        _logger = logger;
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
                await ProcessEvent(playerScoreAdded.Id, @event, eventMetadata, cancellationToken);
                break;
            case PlayerScoreUpdated playerScoreUpdated:
                await ProcessEvent(playerScoreUpdated.Id, @event, eventMetadata, cancellationToken);
                break;
        }
    }

    private async Task ProcessEvent<TEvent>(
        string playerId,
        TEvent @event,
        EventMetadata eventMetadata,
        CancellationToken cancellationToken
    )
        where TEvent : IDomainEvent
    {
        bool isDesc = true;
        double updatedScore = 0;
        double previousScore = 0;

        var strategy = _leaderBoardReadDbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // https://www.thinktecture.com/en/entity-framework-core/use-transactionscope-with-caution-in-2-1/
            // https://github.com/dotnet/efcore/issues/6233#issuecomment-242693262
            var transaction = await _leaderBoardReadDbContext.Database.BeginTransactionAsync(
                cancellationToken
            );

            try
            {
                var entity = await _leaderBoardReadDbContext.FindAsync<PlayerScoreReadModel>(
                    playerId,
                    cancellationToken
                );

                // if entity not exists, add it to DbContext
                if (entity is null)
                {
                    entity = (PlayerScoreReadModel)
                        Activator.CreateInstance(typeof(PlayerScoreReadModel), true)!;

                    previousScore = 0;

                    entity.When(@event);

                    updatedScore = entity.Score;

                    await _leaderBoardReadDbContext
                        .Set<PlayerScoreReadModel>()
                        .AddAsync(entity, cancellationToken);
                }
                else
                {
                    previousScore = entity.Score;

                    entity.When(@event);

                    updatedScore = entity.Score;
                }

                var eventLogPosition = eventMetadata.LogPosition;

                if (entity.LastProcessedPosition >= eventLogPosition)
                    return;

                entity.LastProcessedPosition = eventLogPosition;

                await _leaderBoardReadDbContext
                    .SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);

                var rangeMembersToNotifyTask = _redisDatabase.SortedSetRangeByScoreAsync(
                    entity.LeaderBoardName,
                    previousScore,
                    updatedScore,
                    exclude: Exclude.None,
                    order: isDesc ? Order.Descending : Order.Ascending
                );

                var rangeMembersToNotify = await rangeMembersToNotifyTask;

                var playerIds = rangeMembersToNotify
                    .Select(x => x.ToString().Split(":")[1])
                    .ToList();

                if (playerIds.Any())
                {
                    // publish to use in the signalr real-time notification
                    await _busPublisher.Publish(
                        new PlayersRankAffected(playerIds),
                        cancellationToken
                    );
                }

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Transaction completed successfully");
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Transaction Rolled back, error: {Message}", e.Message);
            }
        });
    }
}
