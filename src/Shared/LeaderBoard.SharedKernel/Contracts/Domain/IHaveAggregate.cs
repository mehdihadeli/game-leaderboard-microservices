using LeaderBoard.SharedKernel.Contracts.Domain.Events;

namespace LeaderBoard.SharedKernel.Contracts.Domain;

public interface IHaveAggregate : IHaveDomainEvents, IHaveAggregateVersion { }
