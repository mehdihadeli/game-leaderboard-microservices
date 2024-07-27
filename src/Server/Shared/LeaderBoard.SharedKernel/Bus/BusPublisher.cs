using LeaderBoard.SharedKernel.Application.Data.EFContext;
using MassTransit;

namespace LeaderBoard.SharedKernel.Bus;

public class BusPublisher : IBusPublisher
{
    private readonly InboxOutboxDbContext _inboxOutboxDbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    public BusPublisher(InboxOutboxDbContext inboxOutboxDbContext, IPublishEndpoint publishEndpoint)
    {
        _inboxOutboxDbContext = inboxOutboxDbContext;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Publish<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        await _publishEndpoint.Publish(message, cancellationToken);
        await _inboxOutboxDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task PublishBatch<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default)
        where T : class
    {
        await _publishEndpoint.PublishBatch(messages, cancellationToken);
        await _inboxOutboxDbContext.SaveChangesAsync(cancellationToken);
    }
}
