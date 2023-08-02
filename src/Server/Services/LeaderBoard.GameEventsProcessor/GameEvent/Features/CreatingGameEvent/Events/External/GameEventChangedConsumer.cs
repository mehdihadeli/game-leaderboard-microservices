using LeaderBoard.GameEventsProcessor.PlayerScores.Features.AddingOrUpdatingPlayerScore;
using LeaderBoard.GameEventsProcessor.Shared;
using LeaderBoard.SharedKernel.Application.Messages;
using MassTransit;
using MediatR;

namespace LeaderBoard.GameEventsProcessor.GameEvent.Features.CreatingGameEvent.Events.External;

public class GameEventChangedConsumer : IConsumer<GameEventChanged>
{
    private readonly ILogger<GameEventChangedConsumer> _logger;
    private readonly IMediator _mediator;

    public GameEventChangedConsumer(ILogger<GameEventChangedConsumer> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<GameEventChanged> context)
    {
        _logger.LogInformation("Message {Message} as a external event received", context.Message);
        var command = new AddOrUpdatePlayerScore(
            Guid.Parse(context.Message.PlayerId),
            context.Message.Score,
            Constants.GlobalLeaderBoard,
            context.Message.FirstName,
            context.Message.LastName,
            context.Message.Country
        );

        await _mediator.Send(command);
    }
}
