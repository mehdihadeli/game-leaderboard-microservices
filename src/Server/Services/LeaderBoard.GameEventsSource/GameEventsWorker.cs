using Bogus;
using LeaderBoard.GameEventsSource.GameEvent.Features;
using LeaderBoard.GameEventsSource.GameEvent.Features.CreatingGameEvent;
using LeaderBoard.GameEventsSource.Players.Models;
using LeaderBoard.GameEventsSource.Shared.Data.EFDbContext;
using MediatR;
using Microsoft.AspNetCore.Identity;
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
        using var scope = _serviceProvider.CreateScope();
        var gameEventSourceDbContext =
            scope.ServiceProvider.GetRequiredService<GameEventSourceDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Player>>();

        var tempIteration = 0;
        while (!stoppingToken.IsCancellationRequested && _gameEventsOptions.EnablePublishWorker)
        {
            tempIteration++;
            var randomList = new List<Player>();
            var random = new Random();
            var count = gameEventSourceDbContext.Players.Count();
            if (count == 0)
                continue;

            // Generate a random index
            var randomIndex = random.Next(maxValue: count);

            // Retrieve the random player
            var randomPlayer = gameEventSourceDbContext.Players.Skip(randomIndex).FirstOrDefault();
            if (randomPlayer is not null)
                randomList.Add(randomPlayer);

            var user1 = await userManager.FindByNameAsync("test");
            if (user1 is not null && tempIteration == 5)
                randomList.Add(user1);

            var user2 = await userManager.FindByNameAsync("mehdi");
            if (user2 is not null && tempIteration == 5)
                randomList.Add(user2);

            var user = randomList[new Random().Next(0, randomList.Count - 1)];
            int score = 0;

            score = tempIteration == 5 ? new Faker().Random.Int(10, 100) : new Faker().Random.Int(100, 500);

            await mediator.Send(
                new CreateGameEvent(user.Id, score, user.FirstName, user.LastName, user.Country),
                stoppingToken
            );

            await Task.Delay(TimeSpan.FromSeconds(_gameEventsOptions.PublishDelay), stoppingToken);
            if (tempIteration == 5)
            {
                tempIteration = 0;
            }
        }
    }
}
