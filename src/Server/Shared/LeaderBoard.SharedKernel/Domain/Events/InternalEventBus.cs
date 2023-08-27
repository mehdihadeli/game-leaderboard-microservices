using System.Collections.Concurrent;
using System.Reflection;
using LeaderBoard.SharedKernel.Contracts.Data.EventStore;
using LeaderBoard.SharedKernel.Contracts.Domain.Events;
using MediatR;
using Polly;

namespace LeaderBoard.SharedKernel.Domain.Events;

public class InternalEventBus : IInternalEventBus
{
	private readonly IMediator _mediator;
	private readonly AsyncPolicy _policy;
	private static readonly ConcurrentDictionary<Type, MethodInfo> PublishMethods = new();

	public InternalEventBus(IMediator mediator, AsyncPolicy policy)
	{
		_mediator = mediator;
		_policy = policy;
	}

	public Task Publish(IStreamEvent eventEnvelope, CancellationToken ct)
	{
		// calling generic `Publish<T>` in `InternalEventBus` class
		var genericPublishMethod = PublishMethods.GetOrAdd(
			eventEnvelope.Data.GetType(),
			eventType =>
				typeof(InternalEventBus)
					.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
					.Single(m => m.Name == nameof(Publish) && m.GetGenericArguments().Any())
					.MakeGenericMethod(eventType));

		return (Task) genericPublishMethod.Invoke(this, new object[] {eventEnvelope, ct})!;
	}

	public async Task Publish<T>(IStreamEvent<T> eventEnvelope, CancellationToken ct)
	where T : IDomainEvent
	{
		await _policy.ExecuteAsync(c => _mediator.Publish(eventEnvelope.Data, c), ct);
	}

	public async Task Publish(IEvent @event, CancellationToken ct)
	{
		await _policy.ExecuteAsync(c => _mediator.Publish(@event, c), ct);
	}
}