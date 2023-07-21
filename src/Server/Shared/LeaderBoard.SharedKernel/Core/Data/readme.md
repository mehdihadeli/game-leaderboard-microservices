#### Migration Scripts

```bash
dotnet ef migrations add InitialLeaderboardMigration -o Infrastructure/Data/EFContext/Migrations -c LeaderBoardDBContext
dotnet ef database update -c LeaderBoardDBContext
```
