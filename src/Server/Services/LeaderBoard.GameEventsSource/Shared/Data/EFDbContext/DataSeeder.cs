using AutoBogus;
using Bogus;
using LeaderBoard.GameEventsSource.Players.Models;
using LeaderBoard.SharedKernel.Contracts.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LeaderBoard.GameEventsSource.Shared.Data.EFDbContext;

public class DataSeeder : ISeeder
{
    private readonly UserManager<Player> _userManager;
    private readonly GameEventSourceDbContext _gameEventSourceDbContext;

    public DataSeeder(UserManager<Player> userManager, GameEventSourceDbContext gameEventSourceDbContext)
    {
        _userManager = userManager;
        _gameEventSourceDbContext = gameEventSourceDbContext;
    }

    public async Task SeedAsync()
    {
        if (!await _gameEventSourceDbContext.Players.AnyAsync())
        {
            //Seed Default User
            var firstUser = new Player
            {
                FirstName = "mehdi",
                LastName = "mehdi",
                Country = "Iran",
                UserName = "mehdi",
                Email = "mehdi@test.com",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            var secondUser = new Player
            {
                FirstName = "test",
                LastName = "test",
                Country = "Iran",
                UserName = "test",
                Email = "test@test.com",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            await _userManager.CreateAsync(firstUser, "000000");
            await _userManager.CreateAsync(secondUser, "000000");

            var players = new Faker<Player>()
                .RuleFor(x => x.FirstName, f => f.Name.FirstName())
                .RuleFor(x => x.LastName, f => f.Name.LastName())
                .RuleFor(x => x.Country, f => f.Address.Country())
                .RuleFor(x => x.UserName, f => f.Internet.UserName())
                .RuleFor(x => x.Email, f => f.Internet.Email())
                .RuleFor(x => x.EmailConfirmed, f => true)
                .RuleFor(x => x.PhoneNumberConfirmed, f => true)
                .RuleFor(x => x.Id, f => f.Random.Guid())
                .Generate(100);

            // https://code-maze.com/dotnet-fast-inserts-entity-framework-ef-core/
            //await _gameEventSourceDbContext.BulkInsertAsync(players);

            foreach (var player in players)
            {
                await _userManager.CreateAsync(player, "000000");
            }
        }
    }
}
