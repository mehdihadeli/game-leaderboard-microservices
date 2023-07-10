using LeaderBoard.SharedKernel.Data;

namespace LeaderBoard.Infrastructure.Data.EFContext;

public class CatalogsDbContextDesignFactory : DbContextDesignFactoryBase<LeaderBoardDBContext>
{
    public CatalogsDbContextDesignFactory()
        : base($"{nameof(PostgresOptions)}:{nameof(PostgresOptions.ConnectionString)}") { }
}
