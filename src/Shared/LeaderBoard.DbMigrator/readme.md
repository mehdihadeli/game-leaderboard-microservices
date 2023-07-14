#### Migration Scripts

```bash
dotnet ef migrations add InitialLeaderboardMigration -o Migrations -c LeaderBoardDBContext
dotnet ef database update -c LeaderBoardDBContext
```
