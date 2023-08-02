using System.Reflection;
using Humanizer;
using LeaderBoard.DbMigrator;
using LeaderBoard.GameEventsProcessor.GameEvent.Features.CreatingGameEvent.Events.External;
using LeaderBoard.GameEventsProcessor.PlayerScores.Features.AddingOrUpdatingPlayerScore;
using LeaderBoard.GameEventsProcessor.PlayerScores.Features.GettingGlobalScoreAdnRank;
using LeaderBoard.GameEventsProcessor.PlayerScores.Features.GettingPlayerGroupGlobalScoresAndRanks;
using LeaderBoard.GameEventsProcessor.PlayerScores.Features.GettingRangeScoresAndRanks;
using LeaderBoard.GameEventsProcessor.Shared;
using LeaderBoard.GameEventsProcessor.Shared.Data;
using LeaderBoard.GameEventsProcessor.Shared.Extensions.WebApplicationBuilderExtensions;
using LeaderBoard.GameEventsProcessor.Shared.LocalRedisMessage;
using LeaderBoard.GameEventsProcessor.Shared.Services;
using LeaderBoard.SharedKernel.Application.Data.EFContext;
using LeaderBoard.SharedKernel.Application.Messages;
using LeaderBoard.SharedKernel.Application.Messages.PlayerScore;
using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.SharedKernel.Bus;
using LeaderBoard.SharedKernel.Contracts.Data;
using LeaderBoard.SharedKernel.Contracts.Domain.Events;
using LeaderBoard.SharedKernel.Core.Exceptions;
using LeaderBoard.SharedKernel.Domain.Events;
using LeaderBoard.SharedKernel.EventStoreDB.Extensions;
using LeaderBoard.SharedKernel.OpenTelemetry;
using LeaderBoard.SharedKernel.Postgres;
using LeaderBoard.SharedKernel.Redis;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using StackExchange.Redis;

// https://github.com/serilog/serilog-aspnetcore#two-stage-initialization
Log.Logger = new LoggerConfiguration().MinimumLevel
    .Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithExceptionDetails()
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

    var policy = Policy.Handle<Exception>().RetryAsync(2);
    builder.Services.AddSingleton(ActivityScope.Instance);
    AddInternalEventBus(builder.Services, policy);

    builder.Services.AddEventStoreDB(policy, Assembly.GetExecutingAssembly());

    builder.Services.AddTransient<
        IAggregatesDomainEventsRequestStore,
        AggregatesDomainEventsStore
    >();

    builder.AddPostgresDbContext<LeaderBoardReadDbContext>(
        migrationAssembly: typeof(MigrationRootMetadata).Assembly
    );
    builder.AddPostgresDbContext<InboxOutboxDbContext>(
        migrationAssembly: typeof(MigrationRootMetadata).Assembly
    );

    builder.Services.AddTransient<ISeeder, DataSeeder>();
    builder.Services.AddTransient<IPlayerScoreService, PlayerScoreService>();

    builder.AddCustomHttpClients();

    builder.Services.AddMassTransit(x =>
    {
        // setup masstransit for outbox and producing messages through `IPublishEndpoint`
        x.AddEntityFrameworkOutbox<InboxOutboxDbContext>(o =>
        {
            o.QueryDelay = TimeSpan.FromSeconds(1);
            o.UsePostgres();
            o.UseBusOutbox();

            o.DuplicateDetectionWindow = TimeSpan.FromSeconds(60);
        });

        x.SetKebabCaseEndpointNameFormatter();
        x.AddConsumer<GameEventChangedConsumer>();

        x.UsingRabbitMq(
            (context, cfg) =>
            {
                cfg.AutoStart = true;
                cfg.ConfigureEndpoints(context);

                // https://masstransit-project.com/usage/exceptions.html#retry
                // https://markgossa.com/2022/06/masstransit-exponential-back-off.html
                cfg.UseMessageRetry(r =>
                {
                    r.Exponential(
                            3,
                            TimeSpan.FromMilliseconds(200),
                            TimeSpan.FromMinutes(120),
                            TimeSpan.FromMilliseconds(200)
                        )
                        .Ignore<ValidationException>(); // don't retry if we have invalid data and message goes to _error queue masstransit
                });
            }
        );
    });
    builder.Services.AddScoped<IBusPublisher, BusPublisher>();

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

    var scoreGroup = app.MapGroup("global-board/scores")
        .WithTags(nameof(PlayerScoreReadModel).Pluralize());
    scoreGroup.MapGetGlobalScoreAndRankEndpoint();
    scoreGroup.MapGetPlayerGroupGlobalScoresAndRanksEndpoint();
    scoreGroup.MapGetRangeScoresAndRanksEndpoint();
    scoreGroup.MapAddOrUpdatePlayerScoreEndpoint();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    using (var scope = app.Services.CreateScope())
    {
        var leaderBoardDbContext =
            scope.ServiceProvider.GetRequiredService<LeaderBoardReadDbContext>();
        await leaderBoardDbContext.Database.MigrateAsync();

        var inboxOutboxDbContext = scope.ServiceProvider.GetRequiredService<InboxOutboxDbContext>();
        await inboxOutboxDbContext.Database.MigrateAsync();

        var seeders = scope.ServiceProvider.GetServices<ISeeder>();
        foreach (var seeder in seeders)
            await seeder.SeedAsync();
    }

    var redisDatabase = app.Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase();

    // publish to use in the signalr real-time notification
    await redisDatabase.SubscribeMessage<RedisScoreChangedMessage>(
        RedisScoreChangedMessage.ChannelName,
        async (chanName, message) =>
        {
            using var scope = app.Services.CreateScope();
            var busPublisher = scope.ServiceProvider.GetRequiredService<IBusPublisher>();

            var rangeMembersToNotifyTask = redisDatabase.SortedSetRangeByScoreAsync(
                message.LeaderBoardName,
                message.PreviousScore,
                message.UpdatedScore,
                exclude: Exclude.None,
                order: message.IsDesc ? Order.Descending : Order.Ascending
            );

            var rangeMembersToNotify = await rangeMembersToNotifyTask;

            var playerIds = rangeMembersToNotify.Select(x => x.ToString().Split(":")[1]).ToList();

            if (playerIds.Any())
                await busPublisher.Publish(new PlayersRankAffected(playerIds));
        }
    );

    await redisDatabase.SubscribeMessage<RedisLocalAddOrUpdatePlayerMessage>(
        RedisLocalAddOrUpdatePlayerMessage.ChannelName,
        async (_, message) =>
        {
            using var scope = app.Services.CreateScope();
            var busPublisher = scope.ServiceProvider.GetRequiredService<IBusPublisher>();

            var playerScoreAdded = new PlayerScoreAddOrUpdated(
                message!.PlayerId,
                message.Score,
                message.LeaderBoardName,
                message.FirstName,
                message.LastName,
                message.Country
            );

            await busPublisher.Publish(playerScoreAdded);
        }
    );

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
