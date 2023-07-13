namespace LeaderBoard.Workers.WriteBehind;

// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services
public class ProduceEventsWorker : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
