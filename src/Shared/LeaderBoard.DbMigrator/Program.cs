using LeaderBoard.DbMigrator;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("Starting database migration...");

var leaderboardContextFactory = new LeaderboardDbContextDesignFactory();
var leaderBoardContext = leaderboardContextFactory.CreateDbContext(new string[] { });
await leaderBoardContext.Database.MigrateAsync();

var inboxOutboxDbContextDesignFactory = new InboxOutboxDbContextDesignFactory();
var inboxOutboxDbContext = inboxOutboxDbContextDesignFactory.CreateDbContext(new string[] { });
await inboxOutboxDbContext.Database.MigrateAsync();

Console.WriteLine("Database migration completed...");
