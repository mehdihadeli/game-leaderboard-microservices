using System.Reflection;
using Humanizer;
using LeaderBoard.DbMigrator;
using LeaderBoard.ReadThrough.Data;
using LeaderBoard.ReadThrough.Endpoints.GettingRangeScoresAndRanks;
using LeaderBoard.ReadThrough.Extensions.WebApplicationBuilderExtensions;
using LeaderBoard.ReadThrough.Providers;
using LeaderBoard.ReadThrough.Services;
using LeaderBoard.SharedKernel.Application.Data.EFContext;
using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.SharedKernel.Core.Extensions.ServiceCollectionExtensions;
using LeaderBoard.SharedKernel.Data;
using LeaderBoard.SharedKernel.Data.Contracts;
using LeaderBoard.SharedKernel.Data.Postgres;
using LeaderBoard.SharedKernel.Redis;
using LeaderBoard.WriteBehind;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

// https://github.com/serilog/serilog-aspnetcore#two-stage-initialization
Log.Logger = new LoggerConfiguration().MinimumLevel
    .Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

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

    builder.Host.UseSerilog(
        (context, services, configuration) =>
        {
            //https://github.com/serilog/serilog-aspnetcore#two-stage-initialization
            configuration.ReadFrom
                .Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console();
        }
    );

    builder.AddAppProblemDetails();

    builder.Services.AddValidatedOptions<ReadThroughOptions>();

    // Add services to the container.
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

    builder.AddCustomRedis();

    builder.AddPostgresDbContext<LeaderBoardDbContext>(
        migrationAssembly: typeof(MigrationRootMetadata).Assembly
    );
    builder.Services.AddTransient<ISeeder, DataSeeder>();

    builder.Services.AddScoped<IReadThrough, ReadThrough>();
    builder.Services.AddScoped<IReadProviderDatabase, PostgresReadProviderDatabase>();

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

    app.UseHttpsRedirection();

    var scoreGroup = app.MapGroup("global-board/scores").WithTags(nameof(PlayerScore).Pluralize());
    scoreGroup.MapGetRangeScoresAndRanks();
    scoreGroup.MapGetGlobalScoreAndRank();
    scoreGroup.MapGetPlayerGroupScoresAndRanks();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    using (var scope = app.Services.CreateScope())
    {
        var leaderBoardDbContext = scope.ServiceProvider.GetRequiredService<LeaderBoardDbContext>();
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
