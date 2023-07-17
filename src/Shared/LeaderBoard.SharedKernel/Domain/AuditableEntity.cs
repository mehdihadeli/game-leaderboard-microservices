using LeaderBoard.SharedKernel.Contracts.Domain;

namespace LeaderBoard.SharedKernel.Domain;

public class AuditableEntity<TId> : Entity<TId>, IAuditableEntity<TId>
{
    public DateTime? LastModified { get; protected set; } = default!;
    public int? LastModifiedBy { get; protected set; } = default!;
}

public abstract class AuditableEntity<TIdentity, TId> : AuditableEntity<TIdentity>
    where TIdentity : Identity<TId> { }

public class AuditableEntity : AuditableEntity<Identity<long>, long> { }
