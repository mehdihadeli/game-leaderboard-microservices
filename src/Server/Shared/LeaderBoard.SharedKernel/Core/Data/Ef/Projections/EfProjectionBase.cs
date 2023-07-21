using LeaderBoard.SharedKernel.Contracts.Data.EventStore.Projections;
using LeaderBoard.SharedKernel.Contracts.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace LeaderBoard.SharedKernel.Core.Data.Ef.Projections;

public abstract class EfProjectionBase<TDbContext, TView> : IReadProjection
    where TView : class, IVersionedProjection
    where TDbContext : DbContext
{
    private readonly TDbContext _context;

    protected EfProjectionBase(TDbContext context)
    {
        _context = context;
    }

    protected abstract Guid GetId(IDomainEvent domainEvent);

    public virtual async Task ProjectAsync<TEvent>(
        IEventEnvelope<TEvent> eventEnvelope,
        CancellationToken cancellationToken = default
    )
        where TEvent : IDomainEvent
    {
        var @event = eventEnvelope.Data;
        var eventMetadata = eventEnvelope.Metadata;

        var id = GetId(@event);

        var entity = await _context.FindAsync<TView>(id, cancellationToken);

        if (entity is null)
        {
            entity = (TView)Activator.CreateInstance(typeof(TView), true)!;
            await _context.Set<TView>().AddAsync(entity, cancellationToken);
        }

        var eventLogPosition = eventMetadata.LogPosition;

        if (entity.LastProcessedPosition >= eventLogPosition)
            return;

        entity.When(@event);

        entity.LastProcessedPosition = eventLogPosition;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

internal class EfProjectionBase<TDbContext, TEvent, TView> : IReadProjection<TEvent>
    where TView : class, IVersionedProjection
    where TEvent : IDomainEvent
    where TDbContext : DbContext
{
    private readonly TDbContext _context;
    private readonly Func<TEvent, Guid> _getId;

    public EfProjectionBase(TDbContext context, Func<TEvent, Guid> getId)
    {
        _context = context;
        _getId = getId;
    }

    public async Task ProjectAsync(
        IEventEnvelope<TEvent> eventEnvelope,
        CancellationToken cancellationToken = default
    )
    {
        var @event = eventEnvelope.Data;
        var eventMetadata = eventEnvelope.Metadata;

        var id = _getId(@event);

        var entity = await _context.FindAsync<TView>(id, cancellationToken);

        if (entity is null)
        {
            entity = (TView)Activator.CreateInstance(typeof(TView), true)!;
            await _context.Set<TView>().AddAsync(entity, cancellationToken);
        }

        var eventLogPosition = eventMetadata.LogPosition;

        if (entity.LastProcessedPosition >= eventLogPosition)
            return;

        entity.When(@event);

        entity.LastProcessedPosition = eventLogPosition;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
