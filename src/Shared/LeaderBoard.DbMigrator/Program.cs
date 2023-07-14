using LeaderBoard.DbMigrator;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("Starting database migration...");

var contextFactory = new CatalogsDbContextDesignFactory();
var context = contextFactory.CreateDbContext(new string[] { });
await context.Database.MigrateAsync();

Console.WriteLine("Database migration completed...");
