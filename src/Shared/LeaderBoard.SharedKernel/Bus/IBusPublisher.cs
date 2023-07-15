namespace LeaderBoard.SharedKernel.Bus;

public interface IBusPublisher
{
    Task Publish<T>(T message, CancellationToken cancellationToken = default)
        where T : class;
}
