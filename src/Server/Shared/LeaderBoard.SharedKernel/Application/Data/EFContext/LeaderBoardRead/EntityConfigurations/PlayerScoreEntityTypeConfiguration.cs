using Humanizer;
using LeaderBoard.SharedKernel.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaderBoard.SharedKernel.Application.Data.EFContext.LeaderBoard.EntityConfigurations;

public class PlayerScoreReadModelEntityTypeConfiguration : IEntityTypeConfiguration<PlayerScoreReadModel>
{
    public void Configure(EntityTypeBuilder<PlayerScoreReadModel> builder)
    {
        builder.ToTable(nameof(PlayerScoreReadModel).Underscore());

        builder.HasKey(x => x.PlayerId);
        builder.Property(x => x.PlayerId).HasMaxLength(50);

        builder.Property(x => x.LeaderBoardName).HasMaxLength(50).IsRequired(true);

        builder.Property(x => x.FirstName).HasMaxLength(50);

        builder.Property(x => x.LastName).HasMaxLength(50);

        builder.Property(x => x.Country).HasMaxLength(100);

        builder.Property(x => x.Score).IsRequired();
    }
}
