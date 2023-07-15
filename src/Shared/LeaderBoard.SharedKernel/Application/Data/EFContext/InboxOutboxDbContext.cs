using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace LeaderBoard.SharedKernel.Application.Data.EFContext;

// we should use `inbox-outbox` in same transaction of our main data model DbContext when we save our data models, so it is better we put both on them in same database (for example using a UnitOfWork middleware). if they are in different database we should use distributed transaction
public class InboxOutboxDbContext : DbContext
{
    public InboxOutboxDbContext(DbContextOptions<InboxOutboxDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // setup masstransit outbox and inbox table
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        base.OnModelCreating(modelBuilder);
    }
}
