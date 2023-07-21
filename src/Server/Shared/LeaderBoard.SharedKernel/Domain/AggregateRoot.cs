using System.Collections.Concurrent;
using System.Collections.Immutable;
using LeaderBoard.SharedKernel.Contracts.Domain;
using LeaderBoard.SharedKernel.Contracts.Domain.Events;
using LeaderBoard.SharedKernel.Core.Exceptions;

namespace LeaderBoard.SharedKernel.Domain;

public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot<TId>
{
    [NonSerialized]
    private readonly ConcurrentQueue<IDomainEvent> _uncommittedDomainEvents = new();

    private const long NewAggregateVersion = 0;

    public long OriginalVersion { get; private set; } = NewAggregateVersion;

    /// <summary>
    /// Add the <paramref name="domainEvent"/> to the aggregate pending changes event.
    /// </summary>
    /// <param name="domainEvent">The domain event.</param>
    protected void AddDomainEvents(IDomainEvent domainEvent)
    {
        if (!_uncommittedDomainEvents.Any(x => Equals(x.EventId, domainEvent.EventId)))
        {
            _uncommittedDomainEvents.Enqueue(domainEvent);
        }
    }

    public bool HasUncommittedDomainEvents()
    {
        return _uncommittedDomainEvents.Any();
    }

    public IReadOnlyList<IDomainEvent> GetUncommittedDomainEvents()
    {
        return _uncommittedDomainEvents.ToImmutableList();
    }

    public void ClearDomainEvents()
    {
        _uncommittedDomainEvents.Clear();
    }

    public IReadOnlyList<IDomainEvent> DequeueUncommittedDomainEvents()
    {
        var events = _uncommittedDomainEvents.ToImmutableList();
        MarkUncommittedDomainEventAsCommitted();
        return events;
    }

    public void MarkUncommittedDomainEventAsCommitted()
    {
        _uncommittedDomainEvents.Clear();
    }

    public void CheckRule(IBusinessRule rule)
    {
        if (rule.IsBroken())
        {
            throw new BusinessRuleValidationException(rule);
        }
    }
}

public abstract class AggregateRoot<TIdentity, TId> : AggregateRoot<TIdentity>
    where TIdentity : Identity<TId> { }

public abstract class AggregateRoot : AggregateRoot<AggregateId, long>, IAggregateRoot { }
