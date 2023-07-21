#### Migration Scripts

```bash
dotnet ef migrations add InitialGameEventSourceMigration -o Shared/Data/Migrations -c GameEventSourceDbContext

dotnet ef database update -c GameEventSourceDbContext
```
