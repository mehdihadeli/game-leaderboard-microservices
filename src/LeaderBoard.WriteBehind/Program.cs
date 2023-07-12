using System.Reflection;
using LeaderBoard.SharedKernel.Core.Extensions;
using LeaderBoard.SharedKernel.Core.Extensions.ServiceCollectionExtensions;
using LeaderBoard.SharedKernel.Data;
using LeaderBoard.SharedKernel.Redis;
using LeaderBoard.WriteBehind;
using LeaderBoard.WriteBehind.Consumers;
using LeaderBoard.WriteBehind.Infrastructure.Data.EFContext;
using LeaderBoard.WriteBehind.Providers;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddValidatedOptions<WriteBehindOptions>();

builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
builder.AddCustomRedis();
builder.AddPostgresDbContext<LeaderBoardDBContext>();
builder.Services.AddScoped<IRedisStreamWriteBehind, RedisStreamWriteBehind>();
builder.Services.AddScoped<IWriteBehindDatabaseProvider, PostgresWriteBehindDatabaseProvider>();

builder.Services.AddHostedService<WriteBehindWorker>();

var options = builder.Configuration.BindOptions<WriteBehindOptions>();
if (options.UseBrokerWriteBehind)
{
    builder.Services.AddMassTransit(x =>
    {
        x.SetKebabCaseEndpointNameFormatter();
        x.AddConsumer<PlayerScoreUpdatedConsumer>();
        x.AddConsumer<PlayerScoreAddedConsumer>();
        x.UsingRabbitMq(
            (context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            }
        );
    });
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var leaderBoardDbContext = scope.ServiceProvider.GetRequiredService<LeaderBoardDBContext>();
    await leaderBoardDbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var summaries = new[]
{
    "Freezing",
    "Bracing",
    "Chilly",
    "Cool",
    "Mild",
    "Warm",
    "Balmy",
    "Hot",
    "Sweltering",
    "Scorching"
};

app.MapGet(
        "/weatherforecast",
        () =>
        {
            var forecast = Enumerable
                .Range(1, 5)
                .Select(
                    index =>
                        new WeatherForecast(
                            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                            Random.Shared.Next(-20, 55),
                            summaries[Random.Shared.Next(summaries.Length)]
                        )
                )
                .ToArray();
            return forecast;
        }
    )
    .WithName("GetWeatherForecast")
    .WithOpenApi();

app.Run();

namespace LeaderBoard.WriteBehind
{
    record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}
