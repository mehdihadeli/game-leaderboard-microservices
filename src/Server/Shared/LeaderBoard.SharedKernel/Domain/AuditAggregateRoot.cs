using LeaderBoard.SharedKernel.Contracts.Domain;

namespace LeaderBoard.SharedKernel.Domain;

public abstract class AuditAggregateRoot<TId> : AggregateRoot<TId>, IAuditableEntity<TId>
{
    public DateTime? LastModified { get; protected set; } = default!;
    public int? LastModifiedBy { get; protected set; } = default!;
}

public abstract class AuditAggregateRoot<TIdentity, TId> : AuditAggregateRoot<TIdentity>
    where TIdentity : Identity<TId> { }

public abstract class AuditAggregateRoot : AuditAggregateRoot<Identity<long>, long> { }
