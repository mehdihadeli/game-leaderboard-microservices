using System.Diagnostics;
using EventStore.Client;
using Grpc.Core;
using LeaderBoard.SharedKernel.Contracts.Data.EventStore.Projections;
using LeaderBoard.SharedKernel.Contracts.Domain.Events;
using LeaderBoard.SharedKernel.Core;
using LeaderBoard.SharedKernel.Domain.Events;
using LeaderBoard.SharedKernel.EventStoreDB.Events;
using LeaderBoard.SharedKernel.OpenTelemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeaderBoard.SharedKernel.EventStoreDB.Subscriptions;

// Ref: https://github.com/oskardudycz/EventSourcing.NetCore

// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services
public class EventStoreDBSubscriptionToAll : BackgroundService
{
    private readonly IInternalEventBus _internalEventBus;
    private readonly EventStoreClient _eventStoreClient;
    private readonly EventTypeMapper _eventTypeMapper;
    private readonly ISubscriptionCheckpointRepository _checkpointRepository;
    private readonly IActivityScope _activityScope;
    private readonly EventStoreDBSubscriptionToAllOptions _options;
    private readonly IProjectionPublisher _projectionPublisher;
    private readonly ILogger<EventStoreDBSubscriptionToAll> _logger;
    private EventStoreDBSubscriptionToAllOptions _subscriptionOptions = default!;
    private string SubscriptionId => _subscriptionOptions.SubscriptionId;
    private readonly object _resubscribeLock = new();
    private CancellationToken _cancellationToken;

    public EventStoreDBSubscriptionToAll(
        EventStoreClient eventStoreClient,
        EventTypeMapper eventTypeMapper,
        IInternalEventBus internalEventBus,
        ISubscriptionCheckpointRepository checkpointRepository,
        IActivityScope activityScope,
        IOptions<EventStoreDBSubscriptionToAllOptions> options,
        IProjectionPublisher projectionPublisher,
        ILogger<EventStoreDBSubscriptionToAll> logger
    )
    {
        _internalEventBus = internalEventBus;
        _eventStoreClient = eventStoreClient;
        _eventTypeMapper = eventTypeMapper;
        _checkpointRepository = checkpointRepository;
        _activityScope = activityScope;
        _options = options.Value;
        _projectionPublisher = projectionPublisher;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return SubscribeToAll(_options, stoppingToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Subscription to all '{SubscriptionId}' stopped", SubscriptionId);
        return base.StopAsync(cancellationToken);
    }

    private async Task SubscribeToAll(
        EventStoreDBSubscriptionToAllOptions subscriptionOptions,
        CancellationToken ct
    )
    {
        // see: https://github.com/dotnet/runtime/issues/36063
        await Task.Yield();

        _subscriptionOptions = subscriptionOptions;
        _cancellationToken = ct;

        _logger.LogInformation(
            "Subscription to all '{SubscriptionId}'",
            subscriptionOptions.SubscriptionId
        );

        var checkpoint = await _checkpointRepository.Load(SubscriptionId, ct).ConfigureAwait(false);

        await _eventStoreClient
            .SubscribeToAllAsync(
                checkpoint == null
                    ? FromAll.Start
                    : FromAll.After(new Position(checkpoint.Value, checkpoint.Value)),
                HandleEvent,
                subscriptionOptions.ResolveLinkTos,
                HandleDrop,
                subscriptionOptions.FilterOptions,
                subscriptionOptions.Credentials,
                ct
            )
            .ConfigureAwait(false);

        _logger.LogInformation("Subscription to all '{SubscriptionId}' started", SubscriptionId);
    }

    private async Task HandleEvent(
        StreamSubscription subscription,
        ResolvedEvent resolvedEvent,
        CancellationToken token
    )
    {
        try
        {
            if (IsEventWithEmptyData(resolvedEvent) || IsCheckpointEvent(resolvedEvent))
                return;

            var streamEvent = resolvedEvent.ToStreamEvent();

            if (streamEvent == null)
            {
                // That can happen if we're sharing database between modules.
                // If we're subscribing to all and not filtering out events from other modules,
                // then we might get events that are from other module and we might not be able to deserialize them.
                // In that case it's safe to ignore deserialization error.
                // You may add more sophisticated logic checking if it should be ignored or not.
                _logger.LogWarning(
                    "Couldn't deserialize event with id: {EventId}",
                    resolvedEvent.Event.EventId
                );

                if (!_subscriptionOptions.IgnoreDeserializationErrors)
                    throw new InvalidOperationException(
                        $"Unable to deserialize event {resolvedEvent.Event.EventType} with id: {resolvedEvent.Event.EventId}"
                    );

                return;
            }

            await _activityScope
                .Run(
                    $"{nameof(EventStoreDBSubscriptionToAll)}/{nameof(HandleEvent)}",
                    async (_, ct) =>
                    {
                        // publish event to internal event bus
                        await _internalEventBus
                            .Publish(streamEvent, _cancellationToken)
                            .ConfigureAwait(false);

                        await _projectionPublisher.PublishAsync(streamEvent, _cancellationToken);

                        await _checkpointRepository
                            .Store(
                                SubscriptionId,
                                resolvedEvent.Event.Position.CommitPosition,
                                _cancellationToken
                            )
                            .ConfigureAwait(false);
                    },
                    new StartActivityOptions
                    {
                        Tags =
                        {
                            { TelemetryTags.EventHandling.Event, streamEvent.Data.GetType() }
                        },
                        Parent = streamEvent.Metadata.PropagationContext?.ActivityContext,
                        Kind = ActivityKind.Consumer
                    },
                    token
                )
                .ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogError(
                "Error consuming message: {ExceptionMessage}{ExceptionStackTrace}",
                e.Message,
                e.StackTrace
            );
            // if you're fine with dropping some events instead of stopping subscription
            // then you can add some logic if error should be ignored
            throw;
        }
    }

    private void HandleDrop(
        StreamSubscription _,
        SubscriptionDroppedReason reason,
        Exception? exception
    )
    {
        if (exception is RpcException { StatusCode: StatusCode.Cancelled })
        {
            _logger.LogWarning(
                "Subscription to all '{SubscriptionId}' dropped by client",
                SubscriptionId
            );

            return;
        }

        _logger.LogError(
            exception,
            "Subscription to all '{SubscriptionId}' dropped with '{StatusCode}' and '{Reason}'",
            SubscriptionId,
            (exception as RpcException)?.StatusCode ?? StatusCode.Unknown,
            reason
        );

        Resubscribe();
    }

    private void Resubscribe()
    {
        // You may consider adding a max resubscribe count if you want to fail process
        // instead of retrying until database is up
        while (true)
        {
            var resubscribed = false;
            try
            {
                Monitor.Enter(_resubscribeLock);

                // No synchronization context is needed to disable synchronization context.
                // That enables running asynchronous method not causing deadlocks.
                // As this is a background process then we don't need to have async context here.
                using (NoSynchronizationContextScope.Enter())
                {
#pragma warning disable VSTHRD002
                    SubscribeToAll(_subscriptionOptions, _cancellationToken)
                        .Wait(_cancellationToken);
#pragma warning restore VSTHRD002
                }

                resubscribed = true;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to resubscribe to all '{SubscriptionId}' dropped with '{ExceptionMessage}{ExceptionStackTrace}'",
                    SubscriptionId,
                    exception.Message,
                    exception.StackTrace
                );
            }
            finally
            {
                Monitor.Exit(_resubscribeLock);
            }

            if (resubscribed)
                break;

            // Sleep between reconnections to not flood the database or not kill the CPU with infinite loop
            // Randomness added to reduce the chance of multiple subscriptions trying to reconnect at the same time
            Thread.Sleep(1000 + new Random((int)DateTime.UtcNow.Ticks).Next(1000));
        }
    }

    private bool IsEventWithEmptyData(ResolvedEvent resolvedEvent)
    {
        if (resolvedEvent.Event.Data.Length != 0)
            return false;

        _logger.LogInformation("Event without data received");
        return true;
    }

    private bool IsCheckpointEvent(ResolvedEvent resolvedEvent)
    {
        if (resolvedEvent.Event.EventType != _eventTypeMapper.ToName<CheckpointStored>())
            return false;

        _logger.LogInformation("Checkpoint event - ignoring");
        return true;
    }
}
