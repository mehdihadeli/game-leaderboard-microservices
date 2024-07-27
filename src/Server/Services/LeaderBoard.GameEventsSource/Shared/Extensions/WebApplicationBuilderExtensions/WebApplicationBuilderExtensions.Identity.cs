using LeaderBoard.GameEventsSource.Players.Models;
using LeaderBoard.GameEventsSource.Shared.Data.EFDbContext;
using LeaderBoard.SharedKernel.Core.Extensions.ServiceCollectionExtensions;
using Microsoft.AspNetCore.Identity;

namespace LeaderBoard.GameEventsSource.Shared.Extensions.WebApplicationBuilderExtensions;

public static partial class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddCustomIdentity(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatedOptions<IdentityOptions>();

        builder
            .Services.AddIdentity<Player, IdentityRole<Guid>>(options =>
            {
                // Password settings.
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 3;
                options.Password.RequiredUniqueChars = 1;

                // Lockout settings.
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings.
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<GameEventSourceDbContext>()
            .AddDefaultTokenProviders();

        return builder;
    }
}
