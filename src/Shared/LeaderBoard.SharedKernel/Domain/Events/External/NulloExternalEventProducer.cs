using LeaderBoard.SharedKernel.Contracts.Domain.Events;

namespace LeaderBoard.SharedKernel.Domain.Events.External;

public class NulloExternalEventProducer : IExternalEventProducer
{
    public Task Publish(IEventEnvelope @event, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
