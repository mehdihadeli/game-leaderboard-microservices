using AutoBogus;
using LeaderBoard.GameEventsSource.Players.Models;
using LeaderBoard.SharedKernel.Contracts.Data;
using Microsoft.EntityFrameworkCore;

namespace LeaderBoard.GameEventsSource.Shared.Data.EFDbContext;

public class DataSeeder : ISeeder
{
    private readonly GameEventSourceDbContext _gameEventSourceDbContext;

    public DataSeeder(GameEventSourceDbContext gameEventSourceDbContext)
    {
        _gameEventSourceDbContext = gameEventSourceDbContext;
    }

    public async Task SeedAsync()
    {
        if (!await _gameEventSourceDbContext.Players.AnyAsync())
        {
            var players = new AutoFaker<Player>()
                .RuleFor(x => x.FirstName, f => f.Name.FirstName())
                .RuleFor(x => x.LastName, f => f.Name.LastName())
                .RuleFor(x => x.Country, f => f.Address.Country())
                .RuleFor(x => x.Id, f => f.Random.Guid())
                .Generate(1500);

            // https://code-maze.com/dotnet-fast-inserts-entity-framework-ef-core/
            await _gameEventSourceDbContext.BulkInsertAsync(players);
        }
    }
}
