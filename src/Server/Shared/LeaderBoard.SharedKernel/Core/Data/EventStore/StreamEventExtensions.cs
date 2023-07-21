using LeaderBoard.SharedKernel.Contracts.Data.EventStore;
using LeaderBoard.SharedKernel.Contracts.Domain.Events;
using LeaderBoard.SharedKernel.Reflection;

namespace LeaderBoard.SharedKernel.Core.Data.EventStore;

public static class StreamEventExtensions
{
    public static IStreamEvent ToStreamEvent(this IDomainEvent domainEvent, IStreamEventMetadata? metadata)
    {
        return ReflectionUtilities.CreateGenericType(
            typeof(StreamEvent<>),
            new[] { domainEvent.GetType() },
            domainEvent,
            metadata
        );
    }
}
