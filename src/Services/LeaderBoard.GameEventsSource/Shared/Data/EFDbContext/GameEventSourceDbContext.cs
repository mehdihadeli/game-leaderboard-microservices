using LeaderBoard.GameEventsSource.Players.Models;
using LeaderBoard.GameEventsSource.Shared.Data.EFDbContext.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace LeaderBoard.GameEventsSource.Shared.Data.EFDbContext;

public class GameEventSourceDbContext : DbContext
{
    public GameEventSourceDbContext(DbContextOptions<GameEventSourceDbContext> options)
        : base(options) { }

    public DbSet<Player> Players { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PlayerEntityTypeConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
