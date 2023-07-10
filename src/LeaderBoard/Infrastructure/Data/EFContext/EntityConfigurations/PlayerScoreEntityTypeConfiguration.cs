using LeaderBoard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaderBoard.Infrastructure.Data.EFContext.EntityConfigurations
{
    public class PlayerScoreEntityTypeConfiguration : IEntityTypeConfiguration<PlayerScore>
    {
        public void Configure(EntityTypeBuilder<PlayerScore> builder)
        {
            builder.ToTable("player_score");

            builder.HasKey(x => x.PlayerId);
            builder.Property(x => x.PlayerId).HasMaxLength(25);

            builder.Property(x => x.FirstName).HasMaxLength(50).IsRequired(false);

            builder.Property(x => x.FirstName).HasMaxLength(50).IsRequired(false);

            builder.Property(x => x.Country).HasMaxLength(50).IsRequired(false);

            builder.Property(x => x.Rank).IsRequired(false);

            builder.Property(x => x.Score).IsRequired();
        }
    }
}
