using Humanizer;
using LeaderBoard.WriteThrough.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaderBoard.WriteThrough.Infrastructure.Data.EFContext.EntityConfigurations;

public class PlayerScoreEntityTypeConfiguration : IEntityTypeConfiguration<PlayerScore>
{
    public void Configure(EntityTypeBuilder<PlayerScore> builder)
    {
        builder.ToTable(nameof(PlayerScore).Underscore());

        builder.HasKey(x => x.PlayerId);
        builder.Property(x => x.PlayerId).HasMaxLength(50);

        builder.Property(x => x.LeaderBoardName).HasMaxLength(50).IsRequired(true);

        builder.Property(x => x.FirstName).HasMaxLength(50).IsRequired(false);

        builder.Property(x => x.LastName).HasMaxLength(50).IsRequired(false);

        builder.Property(x => x.Country).HasMaxLength(100).IsRequired(false);

        builder.Property(x => x.Rank).IsRequired(false);

        builder.Property(x => x.Score).IsRequired();
    }
}
