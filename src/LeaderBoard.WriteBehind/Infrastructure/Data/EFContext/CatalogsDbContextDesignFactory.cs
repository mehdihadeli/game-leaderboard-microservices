using LeaderBoard.SharedKernel.Data;

namespace LeaderBoard.WriteBehind.Infrastructure.Data.EFContext;

public class CatalogsDbContextDesignFactory : DbContextDesignFactoryBase<LeaderBoardDBContext>
{
    public CatalogsDbContextDesignFactory()
        : base($"{nameof(PostgresOptions)}:{nameof(PostgresOptions.ConnectionString)}") { }
}
