using LeaderBoard.SharedKernel.Contracts.Data.EventStore;
using LeaderBoard.SharedKernel.Contracts.Data.EventStore.Projections;
using LeaderBoard.SharedKernel.Contracts.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace LeaderBoard.SharedKernel.Core.Data.EventStore;

public class ReadProjectionPublisher : IReadProjectionPublisher
{
    private readonly IServiceProvider _serviceProvider;

    public ReadProjectionPublisher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task PublishAsync<T>(IStreamEvent<T> streamEvent, CancellationToken cancellationToken = default)
        where T : IDomainEvent
    {
        using var scope = _serviceProvider.CreateScope();
        var projections = scope.ServiceProvider.GetRequiredService<IEnumerable<IHaveReadProjection>>();
        foreach (var projection in projections)
        {
            await projection.ProjectAsync(streamEvent, cancellationToken);
        }
    }

    public Task PublishAsync(IStreamEvent streamEvent, CancellationToken cancellationToken = default)
    {
        var streamData = streamEvent.Data.GetType();

        var method = typeof(IReadProjectionPublisher)
            .GetMethods()
            .Single(m => m.Name == nameof(PublishAsync) && m.GetGenericArguments().Any())
            .MakeGenericMethod(streamData);

        return (Task)method.Invoke(this, new object[] { streamEvent, cancellationToken })!;
    }
}
