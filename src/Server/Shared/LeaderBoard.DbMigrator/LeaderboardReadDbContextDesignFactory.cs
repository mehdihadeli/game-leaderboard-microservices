using LeaderBoard.SharedKernel.Application.Data.EFContext;
using LeaderBoard.SharedKernel.Postgres;

namespace LeaderBoard.DbMigrator;

public class LeaderboardReadDbContextDesignFactory : DbContextDesignFactoryBase<LeaderBoardReadDbContext>
{
    public LeaderboardReadDbContextDesignFactory()
        : base($"{nameof(PostgresOptions)}:{nameof(PostgresOptions.ConnectionString)}") { }
}
