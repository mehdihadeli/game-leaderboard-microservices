using System.IdentityModel.Tokens.Jwt;
using System.Text;
using LeaderBoard.SharedKernel.Core.Extensions;
using LeaderBoard.SharedKernel.Jwt;
using LeaderBoard.SignalR;
using LeaderBoard.SignalR.Extensions.WebApplicationBuilderExtensions;
using LeaderBoard.SignalR.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration().MinimumLevel
    .Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

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

    // https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/issues/415
    // https://mderriey.com/2019/06/23/where-are-my-jwt-claims/
    // https://leastprivilege.com/2017/11/15/missing-claims-in-the-asp-net-core-2-openid-connect-handler/
    // https://stackoverflow.com/a/50012477/581476
    // to compatibility with new versions of claim names standard
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

    builder.Services.AddSignalR().AddMessagePackProtocol();
    builder.Services.AddSingleton<IUserIdProvider>(new CustomUserIdProvider());

    builder.Services.AddTransient<IHubService, HubService>();

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

    app.UseExceptionHandler(
        options: new ExceptionHandlerOptions { AllowStatusCode404Response = true }
    );

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
