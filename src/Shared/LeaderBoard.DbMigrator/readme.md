#### Migration Scripts

```bash
dotnet ef migrations add InitialLeaderboardMigration -o Migrations/LeaderBoard -c LeaderBoardDBContext
dotnet ef migrations add InitialInboxOutboxMigration -o Migrations/InboxOutbox -c InboxOutboxDbContext

dotnet ef database update -c LeaderBoardDBContext
dotnet ef database update -c InboxOutboxDbContext
```
