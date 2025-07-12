using System.Reflection;
using LeaderBoard.DbMigrator;
using LeaderBoard.SharedKernel.Application.Data.EFContext;
using LeaderBoard.SharedKernel.Bus;
using LeaderBoard.SharedKernel.Contracts.Domain.Events;
using LeaderBoard.SharedKernel.Core.Exceptions;
using LeaderBoard.SharedKernel.Core.Extensions;
using LeaderBoard.SharedKernel.Core.Extensions.ServiceCollectionExtensions;
using LeaderBoard.SharedKernel.Domain.Events;
using LeaderBoard.SharedKernel.EventStoreDB.Extensions;
using LeaderBoard.SharedKernel.OpenTelemetry;
using LeaderBoard.SharedKernel.Postgres;
using LeaderBoard.SharedKernel.Redis;
using LeaderBoard.SharedKernel.Web.ProblemDetail;
using LeaderBoard.WriteBehind;
using LeaderBoard.WriteBehind.Shared;
using LeaderBoard.WriteBehind.Shared.DatabaseProviders;
using LeaderBoard.WriteBehind.Shared.Services.WriteBehindStrategies;
using LeaderBoard.WriteBehind.Shared.Services.WriteBehindStrategies.Broker.Consumers;
using LeaderBoard.WriteBehind.Shared.Services.WriteBehindStrategies.RedisPubSub;
using LeaderBoard.WriteBehind.Shared.Services.WriteBehindStrategies.RedisStream;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;

// https://github.com/serilog/serilog-aspnetcore#two-stage-initialization
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
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
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console();
        }
    );

    builder.Services.AddCustomProblemDetails();

    builder.Services.AddValidatedOptions<WriteBehindOptions>();

    builder.Services.AddAutoMapper(
        cfg => { },
        typeof(WriteBehindRoot).Assembly
    );

    builder.Services.AddMediatR(c => c.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

    var policy = Policy.Handle<Exception>().RetryAsync(2);
    builder.Services.AddSingleton(ActivityScope.Instance);
    AddInternalEventBus(builder.Services, policy);

    builder.Services.AddEventStoreDB(policy, Assembly.GetExecutingAssembly());

    builder.Services.AddTransient<IAggregatesDomainEventsRequestStore, AggregatesDomainEventsStore>();

    builder.AddCustomRedis();

    builder.AddPostgresDbContext<LeaderBoardReadDbContext>(migrationAssembly: typeof(MigrationRootMetadata).Assembly);
    builder.AddPostgresDbContext<InboxOutboxDbContext>(migrationAssembly: typeof(MigrationRootMetadata).Assembly);

    // Register Write Behind Strategies
    builder.Services.AddScoped<IWriteBehind, RedisStreamWriteBehind>();
    builder.Services.AddScoped<IWriteBehind, RedisPubSubWriteBehind>();

    // Register Database Provider
    builder.Services.AddScoped<IWriteBehindDatabaseProvider, EventStoreDbWriteBehindDatabaseProvider>();

    builder.Services.AddHostedService<WriteBehindWorker>();

    var options = builder.Configuration.BindOptions<WriteBehindOptions>();

    builder.Services.AddMassTransit(x =>
    {
        x.AddEntityFrameworkOutbox<InboxOutboxDbContext>(o =>
        {
            o.UsePostgres();
            o.UseBusOutbox();

            o.DuplicateDetectionWindow = TimeSpan.FromSeconds(60);
        });

        x.SetKebabCaseEndpointNameFormatter();
        x.AddConsumer<PlayerScoreAddOrUpdatedConsumer>();
        x.UsingRabbitMq(
            (context, cfg) =>
            {
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

    app.UseExceptionHandler(options: new ExceptionHandlerOptions { AllowStatusCode404Response = true });

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("test"))
    {
        // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/handle-errrors
        app.UseDeveloperExceptionPage();
    }

    using (var scope = app.Services.CreateScope())
    {
        var leaderBoardDbContext = scope.ServiceProvider.GetRequiredService<LeaderBoardReadDbContext>();
        await leaderBoardDbContext.Database.MigrateAsync();

        var inboxOutboxDbContext = scope.ServiceProvider.GetRequiredService<InboxOutboxDbContext>();
        await inboxOutboxDbContext.Database.MigrateAsync();
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

static IServiceCollection AddInternalEventBus(IServiceCollection services, AsyncPolicy? asyncPolicy = null)
{
    services.AddSingleton(sp => new InternalEventBus(
        sp.GetRequiredService<IMediator>(),
        asyncPolicy ?? Policy.NoOpAsync()
    ));
    services.TryAddSingleton<IInternalEventBus>(sp => sp.GetRequiredService<InternalEventBus>());

    return services;
}
