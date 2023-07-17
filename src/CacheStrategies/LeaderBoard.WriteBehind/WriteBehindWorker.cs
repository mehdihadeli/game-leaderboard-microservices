using LeaderBoard.WriteBehind.Services.WriteBehindStrategies;
using StackExchange.Redis;

namespace LeaderBoard.WriteBehind;

// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services
public class WriteBehindWorker : BackgroundService
{
    private readonly IServiceScope _serviceScope;

    public WriteBehindWorker(IServiceProvider serviceProvider)
    {
        _serviceScope = serviceProvider.CreateScope();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var writeBehindStrategies = _serviceScope.ServiceProvider.GetRequiredService<
            IEnumerable<IWriteBehind>
        >();
        List<Task> tasks = new List<Task>();
        foreach (var writeBehindStrategy in writeBehindStrategies)
        {
            // We don't want to await here
            var task = writeBehindStrategy.Execute(stoppingToken);
            tasks.Add(task);
        }

        await Task.WhenAll(tasks.ToArray());
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        var redis = _serviceScope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
        await redis.GetSubscriber().UnsubscribeAllAsync();
        _serviceScope.Dispose();

        await base.StopAsync(cancellationToken);
    }
}
