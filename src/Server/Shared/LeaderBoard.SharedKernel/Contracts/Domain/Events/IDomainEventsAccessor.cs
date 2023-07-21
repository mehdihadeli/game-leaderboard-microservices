namespace LeaderBoard.SharedKernel.Contracts.Domain.Events;

public interface IDomainEventsAccessor
{
    IReadOnlyList<IDomainEvent> UnCommittedDomainEvents { get; }
}
