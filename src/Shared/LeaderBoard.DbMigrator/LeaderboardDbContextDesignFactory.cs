using LeaderBoard.SharedKernel.Application.Data.EFContext;
using LeaderBoard.SharedKernel.Data.Postgres;

namespace LeaderBoard.DbMigrator;

public class LeaderboardDbContextDesignFactory : DbContextDesignFactoryBase<LeaderBoardDbContext>
{
    public LeaderboardDbContextDesignFactory()
        : base($"{nameof(PostgresOptions)}:{nameof(PostgresOptions.ConnectionString)}") { }
}