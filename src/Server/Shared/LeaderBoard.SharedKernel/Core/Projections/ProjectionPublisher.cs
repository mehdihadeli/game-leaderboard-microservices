using System.Collections.Concurrent;
using System.Reflection;
using LeaderBoard.SharedKernel.Contracts.Data.EventStore;
using LeaderBoard.SharedKernel.Contracts.Data.EventStore.Projections;
using LeaderBoard.SharedKernel.Contracts.Domain.Events;
using LeaderBoard.SharedKernel.Core.Data.EventStore;
using LeaderBoard.SharedKernel.Domain.Events;
using LeaderBoard.SharedKernel.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace LeaderBoard.SharedKernel.Core.Projections;

public class ProjectionPublisher : IProjectionPublisher
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly ConcurrentDictionary<Type, MethodInfo> PublishMethods = new();
    private readonly IActivityScope _activityScope;
    private readonly AsyncPolicy _policy;

    public ProjectionPublisher(IServiceProvider serviceProvider, IActivityScope activityScope, AsyncPolicy policy)
    {
        _serviceProvider = serviceProvider;
        _activityScope = activityScope;
        _policy = policy;
    }

    public Task PublishAsync(IStreamEvent eventEnvelope, CancellationToken cancellationToken = default)
    {
        // calling generic `Publish<T>` in `ProjectionPublisher` class
        var genericPublishMethod = PublishMethods.GetOrAdd(
            eventEnvelope.Data.GetType(),
            eventType =>
                typeof(ProjectionPublisher)
                    .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .Single(m => m.Name == nameof(Publish) && m.GetGenericArguments().Any())
                    .MakeGenericMethod(eventType)
        );

        return (Task)genericPublishMethod.Invoke(this, new object[] { eventEnvelope, cancellationToken })!;
    }

    private async Task Publish<TEvent>(StreamEvent<TEvent> streamEvent, CancellationToken ct)
        where TEvent : IDomainEvent
    {
        using var scope = _serviceProvider.CreateScope();

        var eventName = streamEvent.Data.GetType().Name;

        var activityOptions = new StartActivityOptions { Tags = { { TelemetryTags.EventHandling.Event, eventName } } };

        var projections = scope.ServiceProvider.GetServices<IReadProjection>();

        foreach (var projection in projections)
        {
            var activityName = $"{projection.GetType().Name}/{eventName}";

            await _activityScope
                .Run(
                    activityName,
                    (_, token) => _policy.ExecuteAsync(c => projection.ProjectAsync(streamEvent, c), token),
                    activityOptions,
                    ct
                )
                .ConfigureAwait(false);
        }

        var genericReadProjections = scope.ServiceProvider.GetServices<IReadProjection<TEvent>>();
        foreach (var genericReadProjection in genericReadProjections)
        {
            var activityName = $"{genericReadProjection.GetType().Name}/{eventName}";

            await _activityScope
                .Run(
                    activityName,
                    (_, token) => _policy.ExecuteAsync(c => genericReadProjection.ProjectAsync(streamEvent, c), token),
                    activityOptions,
                    ct
                )
                .ConfigureAwait(false);
        }
    }
}
