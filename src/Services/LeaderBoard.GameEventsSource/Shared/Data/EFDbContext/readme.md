#### Migration Scripts

```bash
dotnet ef migrations add InitialGameEventSourceMigration -o Data/Migrations -c GameEventSourceDbContext

dotnet ef database update -c GameEventSourceDbContext
```
