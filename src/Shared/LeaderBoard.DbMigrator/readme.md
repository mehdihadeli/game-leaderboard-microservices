#### Migration Scripts

```bash
dotnet ef migrations add InitialLeaderboardMigration -o Migrations/LeaderBoard -c LeaderBoardReadDbContext
dotnet ef migrations add InitialInboxOutboxMigration -o Migrations/InboxOutbox -c InboxOutboxDbContext

dotnet ef database update -c LeaderBoardReadDbContext
dotnet ef database update -c InboxOutboxDbContext
```
