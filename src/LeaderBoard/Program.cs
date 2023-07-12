using System.Reflection;
using LeaderBoard;
using LeaderBoard.Extensions.WebApplicationBuilderExtensions;
using LeaderBoard.Infrastructure.Data;
using LeaderBoard.Infrastructure.Data.EFContext;
using LeaderBoard.Services;
using LeaderBoard.SharedKernel.Data;
using LeaderBoard.SharedKernel.Data.Contracts;
using LeaderBoard.SharedKernel.Redis;
using LeaderBoard.Workers.WriteBehind;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOptions<LeaderBoardOptions>().BindConfiguration(nameof(LeaderBoardOptions));

builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
builder.Services.AddMediatR(c => c.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

builder.AddCustomRedis();
builder.AddPostgresDbContext<LeaderBoardDBContext>();

builder.Services.AddTransient<ISeeder, DataSeeder>();
builder.Services.AddTransient<IPlayerScoreService, PlayerScoreService>();

builder.AddReadThroughClient();

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq(
        (_, cfg) =>
        {
            cfg.AutoStart = true;
        }
    );
});

// setup workers
builder.Services.AddHostedService<ProduceEventsWorker>();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var leaderBoardDbContext = scope.ServiceProvider.GetRequiredService<LeaderBoardDBContext>();
    await leaderBoardDbContext.Database.MigrateAsync();

    var seeders = scope.ServiceProvider.GetServices<ISeeder>();
    foreach (var seeder in seeders)
        await seeder.SeedAsync();
}

app.Run();
