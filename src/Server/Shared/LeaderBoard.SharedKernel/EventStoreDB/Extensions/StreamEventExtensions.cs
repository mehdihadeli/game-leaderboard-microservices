using EventStore.Client;
using LeaderBoard.SharedKernel.Core.Data.EventStore;

namespace LeaderBoard.SharedKernel.EventStoreDB.Extensions;

public static class StreamEventExtensions
{
    public static IEnumerable<StreamEvent?> ToStreamEvents(this IEnumerable<ResolvedEvent> resolvedEvents)
    {
        return resolvedEvents.Select(x => x.ToStreamEvent());
    }
}
