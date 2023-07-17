namespace LeaderBoard.SharedKernel.Contracts.Domain.Events;

public interface IDomainEventHandler<in TEvent> : IEventHandler<TEvent>
    where TEvent : IDomainEvent { }
