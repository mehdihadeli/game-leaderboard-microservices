using System.Reflection;
using Humanizer;
using LeaderBoard.DbMigrator;
using LeaderBoard.SharedKernel.Application.Data.EFContext;
using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.SharedKernel.Contracts.Domain.Events;
using LeaderBoard.SharedKernel.Domain.Events;
using LeaderBoard.SharedKernel.EventStoreDB.Extensions;
using LeaderBoard.SharedKernel.OpenTelemetry;
using LeaderBoard.SharedKernel.Postgres;
using LeaderBoard.SharedKernel.Redis;
using LeaderBoard.WriteThrough.PlayerScore.Features.AddingOrUpdatingPlayerScore;
using LeaderBoard.WriteThrough.Shared.Extensions.WebApplicationBuilderExtensions;
using LeaderBoard.WriteThrough.Shared.Providers;
using LeaderBoard.WriteThrough.Shared.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly;
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

    // Add services to the container.
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
    builder.Services.AddMediatR(
        c => c.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly())
    );

    var policy = Policy.Handle<Exception>().RetryAsync(2);
    builder.Services.AddSingleton(ActivityScope.Instance);
    AddInternalEventBus(builder.Services, policy);

    builder.Services.AddEventStoreDB(policy, Assembly.GetExecutingAssembly());

    builder.Services.AddTransient<
        IAggregatesDomainEventsRequestStore,
        AggregatesDomainEventsStore
    >();

    builder.AddCustomRedis();

    builder.AddPostgresDbContext<LeaderBoardReadDbContext>(
        migrationAssembly: typeof(MigrationRootMetadata).Assembly
    );

    builder.Services.AddScoped<IWriteThrough, WriteThrough>();
    builder.Services.AddScoped<IWriteProviderDatabase, EventStoreWriteProviderDatabase>();

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

    var scoreGroup = app.MapGroup("global-board/scores")
        .WithTags(nameof(PlayerScoreReadModel).Pluralize());
    scoreGroup.MapAddOrUpdatePlayerScoreEndpoint();

    using (var scope = app.Services.CreateScope())
    {
        var leaderBoardDbContext =
            scope.ServiceProvider.GetRequiredService<LeaderBoardReadDbContext>();
        await leaderBoardDbContext.Database.MigrateAsync();
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

static IServiceCollection AddInternalEventBus(
    IServiceCollection services,
    AsyncPolicy? asyncPolicy = null
)
{
    services.AddSingleton(
        sp =>
            new InternalEventBus(
                sp.GetRequiredService<IMediator>(),
                asyncPolicy ?? Policy.NoOpAsync()
            )
    );
    services.TryAddSingleton<IInternalEventBus>(sp => sp.GetRequiredService<InternalEventBus>());

    return services;
}
