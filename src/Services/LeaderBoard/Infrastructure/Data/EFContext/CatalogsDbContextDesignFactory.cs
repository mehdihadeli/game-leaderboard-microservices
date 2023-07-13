using LeaderBoard.Infrastructure.Data.EFContext;
using LeaderBoard.SharedKernel.Data;

namespace LeaderBoard.Migrations;

public class CatalogsDbContextDesignFactory : DbContextDesignFactoryBase<LeaderBoardDBContext>
{
    public CatalogsDbContextDesignFactory()
        : base($"{nameof(PostgresOptions)}:{nameof(PostgresOptions.ConnectionString)}") { }
}
