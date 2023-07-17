namespace LeaderBoard.SharedKernel.Contracts.Domain;

public interface IAggregateRoot<out TId> : IEntity<TId>, IHaveAggregate { }

public interface IAggregateRoot<out TIdentity, TId> : IAggregateRoot<TIdentity>
    where TIdentity : Identity<TId> { }

public interface IAggregateRoot : IAggregateRoot<AggregateId, long> { }
