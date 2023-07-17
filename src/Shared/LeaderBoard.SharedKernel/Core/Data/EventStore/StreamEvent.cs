using LeaderBoard.SharedKernel.Contracts.Data.EventStore;
using LeaderBoard.SharedKernel.Contracts.Domain.Events;
using LeaderBoard.SharedKernel.Domain.Events;

namespace LeaderBoard.SharedKernel.Core.Data.EventStore;

public record StreamEvent(IDomainEvent Data, IStreamEventMetadata? Metadata = null) : Event, IStreamEvent;

public record StreamEvent<T>(T Data, IStreamEventMetadata? Metadata = null)
    : StreamEvent(Data, Metadata),
        IStreamEvent<T>
    where T : IDomainEvent
{
    public new T Data => (T)base.Data;
}
