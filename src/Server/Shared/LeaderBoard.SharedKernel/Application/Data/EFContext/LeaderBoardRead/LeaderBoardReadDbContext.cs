using LeaderBoard.SharedKernel.Application.Data.EFContext.LeaderBoard.EntityConfigurations;
using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.SharedKernel.Contracts.Data;
using Microsoft.EntityFrameworkCore;

namespace LeaderBoard.SharedKernel.Application.Data.EFContext;

public class LeaderBoardReadDbContext : DbContext
{
    public LeaderBoardReadDbContext(DbContextOptions<LeaderBoardReadDbContext> options)
        : base(options) { }

    public DbSet<PlayerScoreReadModel> PlayerScores { get; set; } = default!;

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
        modelBuilder.ApplyConfiguration(new PlayerScoreReadModelEntityTypeConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
