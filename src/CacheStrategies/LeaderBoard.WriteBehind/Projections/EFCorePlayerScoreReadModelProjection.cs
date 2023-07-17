using LeaderBoard.SharedKernel.Application.Data.EFContext;
using LeaderBoard.SharedKernel.Application.Events;
using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.SharedKernel.Contracts.Data.EventStore.Projections;
using LeaderBoard.SharedKernel.Contracts.Domain.Events;

namespace LeaderBoard.WriteBehind.Projections;

// https://web.archive.org/web/20230128040244/https://zimarev.com/blog/event-sourcing/projections/
public class EFCorePlayerScoreReadModelProjection : IReadProjection
{
    private readonly LeaderBoardReadDbContext _leaderBoardReadDbContext;

    public EFCorePlayerScoreReadModelProjection(LeaderBoardReadDbContext leaderBoardReadDbContext)
    {
        _leaderBoardReadDbContext = leaderBoardReadDbContext;
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
        var entity = await _leaderBoardReadDbContext.FindAsync<PlayerScoreReadModel>(
            playerId,
            cancellationToken
        );

        // if entity not exists, add it to DbContext
        if (entity is null)
        {
            entity = (PlayerScoreReadModel)
                Activator.CreateInstance(typeof(PlayerScoreReadModel), true)!;

            entity.When(@event);

            await _leaderBoardReadDbContext
                .Set<PlayerScoreReadModel>()
                .AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.When(@event);
        }

        var eventLogPosition = eventMetadata.LogPosition;

        if (entity.LastProcessedPosition >= eventLogPosition)
            return;

        entity.LastProcessedPosition = eventLogPosition;

        await _leaderBoardReadDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
