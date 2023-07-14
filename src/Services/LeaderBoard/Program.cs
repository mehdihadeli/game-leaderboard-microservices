using System.Reflection;
using Humanizer;
using LeaderBoard;
using LeaderBoard.DbMigrator;
using LeaderBoard.Endpoints.AddingScorePlayer;
using LeaderBoard.Endpoints.GettingGlobalScoreAdnRank;
using LeaderBoard.Endpoints.GettingPlayerGroupScoresAndRanks;
using LeaderBoard.Endpoints.GettingRangeScoresAndRanks;
using LeaderBoard.Endpoints.UpdatingScore;
using LeaderBoard.Extensions.WebApplicationBuilderExtensions;
using LeaderBoard.Infrastructure.Data;
using LeaderBoard.Services;
using LeaderBoard.SharedKernel.Application.Data.EFContext;
using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.SharedKernel.Data;
using LeaderBoard.SharedKernel.Data.Contracts;
using LeaderBoard.SharedKernel.Redis;
using LeaderBoard.Workers.WriteBehind;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseDefaultServiceProvider(
        (context, options) =>
        {
            var isDevMode =
                context.HostingEnvironment.IsDevelopment()
                || context.HostingEnvironment.IsEnvironment("test")
                || context.HostingEnvironment.IsStaging();

            // Handling Captive Dependency Problem
            // https://ankitvijay.net/2020/03/17/net-core-and-di-beware-of-captive-dependency/
            // https://levelup.gitconnected.com/top-misconceptions-about-dependency-injection-in-asp-net-core-c6a7afd14eb4
            // https://blog.ploeh.dk/2014/06/02/captive-dependency/
            // https://andrewlock.net/new-in-asp-net-core-3-service-provider-validation/
            // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/web-host?view=aspnetcore-7.0&viewFallbackFrom=aspnetcore-2.2#scope-validation
            // CreateDefaultBuilder and WebApplicationBuilder in minimal apis sets `ServiceProviderOptions.ValidateScopes` and `ServiceProviderOptions.ValidateOnBuild` to true if the app's environment is Development.
            // check dependencies are used in a valid life time scope
            options.ValidateScopes = isDevMode;
            // validate dependencies on the startup immediately instead of waiting for using the service
            options.ValidateOnBuild = isDevMode;
        }
    );

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
        migrationAssembly: typeof(MigrationRootMetadata).Assembly
    );

    builder.Services.AddTransient<ISeeder, DataSeeder>();
    builder.Services.AddTransient<IPlayerScoreService, PlayerScoreService>();

    builder.AddCustomHttpClients();

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
