using System.Reflection;
using Humanizer;
using LeaderBoard.SharedKernel.Data;
using LeaderBoard.SharedKernel.Redis;
using LeaderBoard.WriteThrough.Endpoints.PlayerScore.AddingPlayerScore;
using LeaderBoard.WriteThrough.Endpoints.PlayerScore.UpdatingScore;
using LeaderBoard.WriteThrough.Extensions.WebApplicationBuilderExtensions;
using LeaderBoard.WriteThrough.Infrastructure.Data.EFContext;
using LeaderBoard.WriteThrough.Models;
using LeaderBoard.WriteThrough.Providers;
using LeaderBoard.WriteThrough.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

// https://github.com/serilog/serilog-aspnetcore#two-stage-initialization
Log.Logger = new LoggerConfiguration().MinimumLevel
    .Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();
    builder.AddAppProblemDetails();

    // Add services to the container.
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

    builder.AddCustomRedis();

    builder.AddPostgresDbContext<LeaderBoardDBContext>();

    builder.Services.AddScoped<IWriteThrough, WriteThrough>();
    builder.Services.AddScoped<IWriteProviderDatabase, PostgresWriteProviderDatabase>();

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

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    var scoreGroup = app.MapGroup("global-board/scores").WithTags(nameof(PlayerScore).Pluralize());
    scoreGroup.MapAddPlayerScoreEndpoint();
    scoreGroup.MapUpdateScoreEndpoint();

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
