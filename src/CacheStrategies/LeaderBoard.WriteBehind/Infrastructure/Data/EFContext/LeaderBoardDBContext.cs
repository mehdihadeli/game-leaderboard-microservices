using LeaderBoard.SharedKernel.Data.Contracts;
using LeaderBoard.WriteBehind.Infrastructure.Data.EFContext.EntityConfigurations;
using LeaderBoard.WriteBehind.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaderBoard.WriteBehind.Infrastructure.Data.EFContext;

public class LeaderBoardDBContext : DbContext
{
    public DbSet<PlayerScore> PlayerScores { get; set; } = default!;

    public LeaderBoardDBContext(DbContextOptions<LeaderBoardDBContext> options)
        : base(options) { }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var item in ChangeTracker.Entries<IAuditable>().AsEnumerable())
        {
            if (item.State == EntityState.Modified)
            {
                item.Entity.UpdatedAt = DateTime.Now;
            }
            else
            {
                item.Entity.CreatedAt = DateTime.Now;
                item.Entity.UpdatedAt = DateTime.Now;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PlayerScoreEntityTypeConfiguration());
    }
}
