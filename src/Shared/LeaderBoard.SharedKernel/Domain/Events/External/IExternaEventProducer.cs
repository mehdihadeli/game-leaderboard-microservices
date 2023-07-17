using LeaderBoard.SharedKernel.Contracts.Domain.Events;

namespace LeaderBoard.SharedKernel.Domain.Events.External;

public interface IExternalEventProducer
{
    Task Publish(IEventEnvelope @event, CancellationToken ct);
}
