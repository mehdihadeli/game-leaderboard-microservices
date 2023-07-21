using Bogus;
using LeaderBoard.GameEventsSource.GameEvent.Features;
using LeaderBoard.GameEventsSource.Shared.Data.EFDbContext;
using MediatR;
using Microsoft.Extensions.Options;

namespace LeaderBoard.GameEventsSource;

public class GameEventsWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GameEventsWorker> _logger;
    private readonly GameEventSourceOptions _gameEventsOptions;

    public GameEventsWorker(
        IServiceProvider serviceProvider,
        ILogger<GameEventsWorker> logger,
        IOptions<GameEventSourceOptions> gameEventsOptions
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _gameEventsOptions = gameEventsOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && _gameEventsOptions.EnablePublishWorker)
        {
            using var scope = _serviceProvider.CreateScope();
            var gameEventSourceDbContext =
                scope.ServiceProvider.GetRequiredService<GameEventSourceDbContext>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var random = new Random();
            var count = gameEventSourceDbContext.Players.Count();
            if (count == 0)
                continue;

            // Generate a random index
            var randomIndex = random.Next(maxValue: count);

            // Retrieve the random player
            var randomPlayer = gameEventSourceDbContext.Players.Skip(randomIndex).FirstOrDefault();
            if (randomPlayer is null)
                continue;

            var score = new Faker().Random.Double(1, 9999);

            await mediator.Send(
                new CreateGameEvent(
                    randomPlayer.Id,
                    score,
                    randomPlayer.FirstName,
                    randomPlayer.LastName,
                    randomPlayer.Country
                ),
                stoppingToken
            );

            await Task.Delay(TimeSpan.FromSeconds(_gameEventsOptions.PublishDelay), stoppingToken);
        }
    }
}
