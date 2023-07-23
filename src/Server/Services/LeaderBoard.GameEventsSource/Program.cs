using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text;
using Humanizer;
using LeaderBoard.DbMigrator;
using LeaderBoard.GameEventsSource;
using LeaderBoard.GameEventsSource.Accounts.Login;
using LeaderBoard.GameEventsSource.GameEvent.Features;
using LeaderBoard.GameEventsSource.Players.Models;
using LeaderBoard.GameEventsSource.Shared.Data.EFDbContext;
using LeaderBoard.GameEventsSource.Shared.Extensions.WebApplicationBuilderExtensions;
using LeaderBoard.SharedKernel.Application.Data.EFContext;
using LeaderBoard.SharedKernel.Bus;
using LeaderBoard.SharedKernel.Contracts.Data;
using LeaderBoard.SharedKernel.Core.Extensions.ServiceCollectionExtensions;
using LeaderBoard.SharedKernel.Postgres;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
using FluentValidation;
using LeaderBoard.GameEventsSource.Accounts.GettingProfile;
using LeaderBoard.GameEventsSource.Accounts.Logout;
using LeaderBoard.GameEventsSource.GameEvent.Features.CreatingGameEvent;
using LeaderBoard.GameEventsSource.Players.CreatingPlayer;
using LeaderBoard.GameEventsSource.Shared.Services;
using LeaderBoard.SharedKernel.Core.Extensions;
using LeaderBoard.SharedKernel.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog.Exceptions;
using ValidationException = LeaderBoard.SharedKernel.Core.Exceptions.ValidationException;

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
            .Enrich.WithExceptionDetails()
            .WriteTo.Console();
    }
);

builder.AddAppProblemDetails();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddValidatedOptions<GameEventSourceOptions>();

builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
builder.Services.AddMediatR(c => c.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

builder.AddCustomIdentity();

builder.Services.AddValidatedOptions<JwtOptions>();
builder.Services.AddScoped<ITokenService, TokenService>();

// https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/issues/415
// https://mderriey.com/2019/06/23/where-are-my-jwt-claims/
// https://leastprivilege.com/2017/11/15/missing-claims-in-the-asp-net-core-2-openid-connect-handler/
// https://stackoverflow.com/a/50012477/581476
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();
var jwtOptions = builder.Configuration.BindOptions<JwtOptions>();
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Issuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
        };
    });
builder.Services.AddAuthorizationBuilder();

builder.Services.AddHttpContextAccessor();

builder.AddPostgresDbContext<GameEventSourceDbContext>(
    migrationAssembly: Assembly.GetExecutingAssembly()
);
builder.AddPostgresDbContext<InboxOutboxDbContext>(
    migrationAssembly: typeof(MigrationRootMetadata).Assembly
);
builder.Services.AddTransient<ISeeder, DataSeeder>();

builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

builder.Services.AddHostedService<GameEventsWorker>();

builder.Services.AddMassTransit(x =>
{
    // setup masstransit for outbox and producing messages through `IPublishEndpoint`
    x.AddEntityFrameworkOutbox<InboxOutboxDbContext>(o =>
    {
        o.QueryDelay = TimeSpan.FromSeconds(1);
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.UsingRabbitMq(
        (_, cfg) =>
        {
            cfg.AutoStart = true;
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

var policyName = "defaultCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        policyName,
        b =>
        {
            b.WithOrigins("http://localhost:4200") // the Angular app url
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
    );
});

var app = builder.Build();

app.UseExceptionHandler(options: new ExceptionHandlerOptions { AllowStatusCode404Response = true });

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("test"))
{
    // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/handle-errrors
    app.UseDeveloperExceptionPage();
}

app.UseSerilogRequestLogging();

app.UseCors(policyName);

app.UseAuthentication();
app.UseAuthorization();

var identityGroup = app.MapGroup("accounts").WithTags("Accounts");
identityGroup.MapLoginUserEndpoint();
identityGroup.MapLogoutEndpoint();
identityGroup.MapGetProfileEndpoint();

var gameEventGroup = app.MapGroup("game-events").WithTags("GameEvents");
gameEventGroup.MapCreateGameEventEndpoint();

var playersGroup = app.MapGroup("players").WithTags(nameof(Player).Pluralize());
playersGroup.MapCreatePlayerEndpoint();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var leaderBoardDbContext = scope.ServiceProvider.GetRequiredService<GameEventSourceDbContext>();
    await leaderBoardDbContext.Database.MigrateAsync();

    var inboxOutboxDbContext = scope.ServiceProvider.GetRequiredService<InboxOutboxDbContext>();
    await inboxOutboxDbContext.Database.MigrateAsync();

    var seeders = scope.ServiceProvider.GetServices<ISeeder>();
    foreach (var seeder in seeders)
        await seeder.SeedAsync();
}

app.Run();
