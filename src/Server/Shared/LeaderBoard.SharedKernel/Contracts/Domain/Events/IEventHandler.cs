using MediatR;

namespace LeaderBoard.SharedKernel.Contracts.Domain.Events;

public interface IEventHandler<in TEvent> : INotificationHandler<TEvent>
    where TEvent : INotification { }
