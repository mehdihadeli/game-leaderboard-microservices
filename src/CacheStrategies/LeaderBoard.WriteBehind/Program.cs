using System.Reflection;
using LeaderBoard.SharedKernel.Core.Extensions;
using LeaderBoard.SharedKernel.Core.Extensions.ServiceCollectionExtensions;
using LeaderBoard.SharedKernel.Data;
using LeaderBoard.SharedKernel.Redis;
using LeaderBoard.SharedKernel.Web.ProblemDetail;
using LeaderBoard.WriteBehind;
using LeaderBoard.WriteBehind.Consumers;
using LeaderBoard.WriteBehind.Infrastructure.Data.EFContext;
using LeaderBoard.WriteBehind.Providers;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

// https://github.com/serilog/serilog-aspnetcore#two-stage-initialization
Log.Logger = new LoggerConfiguration().MinimumLevel
    .Information()
    .MinimumLevel.Override("MassTransit", LogEventLevel.Debug)
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
    builder.Services.AddCustomProblemDetails();

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
