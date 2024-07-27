using LeaderBoard.SharedKernel.Application.Messages;
using LeaderBoard.SharedKernel.Bus;
using LeaderBoard.SharedKernel.Core.Extensions;
using MediatR;

namespace LeaderBoard.GameEventsSource.GameEvent.Features.CreatingGameEvent;

public record CreateGameEvent(Guid PlayerId, double Score, string FirstName, string LastName, string Country)
    : IRequest;

internal class CreateGameEventHandler : IRequestHandler<CreateGameEvent>
{
    private readonly IBusPublisher _busPublisher;
    private readonly ILogger<CreateGameEventHandler> _logger;

    public CreateGameEventHandler(IBusPublisher busPublisher, ILogger<CreateGameEventHandler> logger)
    {
        _busPublisher = busPublisher;
        _logger = logger;
    }

    public async Task Handle(CreateGameEvent request, CancellationToken cancellationToken)
    {
        request.NotBeNull();

        var message = new GameEventChanged(
            request.PlayerId.ToString(),
            request.Score,
            request.FirstName,
            request.LastName,
            request.Country
        );

        // publish to rabbitmq through outbox
        await _busPublisher.Publish(message, cancellationToken);

        _logger.LogInformation("GameEvent {Message} published to rabbitmq", message);
    }
}
