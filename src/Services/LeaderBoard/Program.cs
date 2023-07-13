using System.Reflection;
using Humanizer;
using LeaderBoard;
using LeaderBoard.Endpoints.AddingScorePlayer;
using LeaderBoard.Endpoints.GettingGlobalScoreAdnRank;
using LeaderBoard.Endpoints.GettingPlayerGroupScoresAndRanks;
using LeaderBoard.Endpoints.GettingRangeScoresAndRanks;
using LeaderBoard.Endpoints.UpdatingScore;
using LeaderBoard.Extensions.WebApplicationBuilderExtensions;
using LeaderBoard.Infrastructure.Data;
using LeaderBoard.Infrastructure.Data.EFContext;
using LeaderBoard.Models;
using LeaderBoard.Services;
using LeaderBoard.SharedKernel.Data;
using LeaderBoard.SharedKernel.Data.Contracts;
using LeaderBoard.SharedKernel.Redis;
using LeaderBoard.SharedKernel.Web;
using LeaderBoard.Workers.WriteBehind;
using MassTransit;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();
    builder.AddAppProblemDetails();

    // Add services to the container.
    builder.Services
        .AddOptions<LeaderBoardOptions>()
        .BindConfiguration(nameof(LeaderBoardOptions));

    // Add services to the container.
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
    builder.Services.AddMediatR(
        c => c.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly())
    );

    builder.AddCustomRedis();

    builder.AddPostgresDbContext<LeaderBoardDBContext>(
        migrationAssembly: Assembly.GetExecutingAssembly()
    );

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

    var app = builder.Build();

    app.UseExceptionHandler(
        options: new ExceptionHandlerOptions { AllowStatusCode404Response = true }
    );

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("test"))
    {
        // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/handle-errrors
        app.UseDeveloperExceptionPage();
    }

    app.UseSerilogRequestLogging();

    var scoreGroup = app.MapGroup("global-board/scores").WithTags(nameof(PlayerScore).Pluralize());
    scoreGroup.MapUpdateScoreEndpoint();
    scoreGroup.MapAddPlayerScoreEndpoint();
    scoreGroup.MapGetPlayerGroupScoresAndRanks();
    scoreGroup.MapGetGlobalScoreAndRank();
    scoreGroup.MapGetRangeScoresAndRanks();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    using (var scope = app.Services.CreateScope())
    {
        var leaderBoardDbContext = scope.ServiceProvider.GetRequiredService<LeaderBoardDBContext>();
        await leaderBoardDbContext.Database.MigrateAsync();

        var seeders = scope.ServiceProvider.GetServices<ISeeder>();
        foreach (var seeder in seeders)
            await seeder.SeedAsync();
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
