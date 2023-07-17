using LeaderBoard.SharedKernel.Application.Data.EFContext;
using LeaderBoard.SharedKernel.Postgres;

namespace LeaderBoard.DbMigrator;

public class InboxOutboxDbContextDesignFactory : DbContextDesignFactoryBase<InboxOutboxDbContext>
{
	public InboxOutboxDbContextDesignFactory()
		: base($"{nameof(PostgresOptions)}:{nameof(PostgresOptions.ConnectionString)}") { }
}