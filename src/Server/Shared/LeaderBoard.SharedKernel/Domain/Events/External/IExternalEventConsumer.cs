namespace LeaderBoard.SharedKernel.Domain.Events.External;

public interface IExternalEventConsumer
{
    Task StartAsync(CancellationToken cancellationToken);
}