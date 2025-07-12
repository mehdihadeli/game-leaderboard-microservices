using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text;
using LeaderBoard.DbMigrator;
using LeaderBoard.SharedKernel.Application.Data.EFContext;
using LeaderBoard.SharedKernel.Bus;
using LeaderBoard.SharedKernel.Core.Exceptions;
using LeaderBoard.SharedKernel.Core.Extensions;
using LeaderBoard.SharedKernel.Jwt;
using LeaderBoard.SharedKernel.Postgres;
using LeaderBoard.SignalR;
using LeaderBoard.SignalR.Consumers;
using LeaderBoard.SignalR.Extensions.WebApplicationBuilderExtensions;
using LeaderBoard.SignalR.Hubs;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithExceptionDetails()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

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

    builder.AddAppProblemDetails();

    // Add services to the container.
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddAutoMapper(
        cfg => { },
        typeof(SignalRRoot).Assembly
    );

    builder.AddCustomHttpClients();

    // https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/issues/415
    // https://mderriey.com/2019/06/23/where-are-my-jwt-claims/
    // https://leastprivilege.com/2017/11/15/missing-claims-in-the-asp-net-core-2-openid-connect-handler/
    // https://stackoverflow.com/a/50012477/581476
    // to compatibility with new versions of claim names standard
    JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
    JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();
    var jwtOptions = builder.Configuration.BindOptions<JwtOptions>();
    builder
        .Services.AddAuthentication(options =>
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

    builder.Services.AddSignalR().AddMessagePackProtocol();
    builder.Services.AddSingleton<IUserIdProvider>(new CustomUserIdProvider());

    builder.Services.AddTransient<IHubService, HubService>();

    builder.AddPostgresDbContext<InboxOutboxDbContext>(migrationAssembly: typeof(MigrationRootMetadata).Assembly);

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
        x.AddConsumer<PlayersRankAffectedConsumer>();

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

    if (app.Environment.IsProduction())
    {
        app.UseExceptionHandler(options: new ExceptionHandlerOptions { AllowStatusCode404Response = true });
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("test"))
    {
        // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/handle-errrors
        app.UseDeveloperExceptionPage();
    }

    app.UseCors(policyName);

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHub<StronglyTypedPlayerScoreHub>("signalr/player-score");

    //https://learn.microsoft.com/en-us/aspnet/core/signalr/hubcontext?view=aspnetcore-8.0
    app.MapGet(
        "hello-clients",
        (IHubService hub) =>
        {
            hub.SendHelloToClients();

            return TypedResults.NoContent();
        }
    );

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    using (var scope = app.Services.CreateScope())
    {
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
