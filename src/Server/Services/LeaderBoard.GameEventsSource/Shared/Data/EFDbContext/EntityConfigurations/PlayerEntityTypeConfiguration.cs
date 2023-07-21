using Humanizer;
using LeaderBoard.GameEventsSource.Players.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaderBoard.GameEventsSource.Shared.Data.EFDbContext.EntityConfigurations;

public class PlayerEntityTypeConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable(nameof(Player).Underscore());

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id);

        builder.Property(x => x.FirstName).HasMaxLength(50);

        builder.Property(x => x.LastName).HasMaxLength(50);

        builder.Property(x => x.Country).HasMaxLength(100);
    }
}
