using LeaderBoard.SharedKernel.Application.Data.EFContext;
using LeaderBoard.SharedKernel.Data;

namespace LeaderBoard.DbMigrator;

public class CatalogsDbContextDesignFactory : DbContextDesignFactoryBase<LeaderBoardDBContext>
{
    public CatalogsDbContextDesignFactory()
        : base($"{nameof(PostgresOptions)}:{nameof(PostgresOptions.ConnectionString)}") { }
}
