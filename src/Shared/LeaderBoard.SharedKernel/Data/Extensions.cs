using System.Reflection;
using LeaderBoard.SharedKernel.Core.Extensions;
using LeaderBoard.SharedKernel.Core.Extensions.ServiceCollectionExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LeaderBoard.SharedKernel.Data;

public static class Extensions
{
    public static WebApplicationBuilder AddPostgresDbContext<TDbContext>(
        this WebApplicationBuilder builder,
        Assembly? migrationAssembly = null,
        Action<DbContextOptionsBuilder>? contextBuilder = null,
        params Assembly[] assembliesToScan
    )
        where TDbContext : DbContext
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        builder.Services.AddValidatedOptions<PostgresOptions>(nameof(PostgresOptions));

        builder.Services.AddDbContext<TDbContext>(
            (sp, options) =>
            {
                var postgresOptions = sp.GetRequiredService<PostgresOptions>();

                options
                    .UseNpgsql(
                        postgresOptions.ConnectionString,
                        sqlOptions =>
                        {
                            var name =
                                migrationAssembly?.GetName().Name
                                ?? postgresOptions.MigrationAssembly
                                ?? typeof(TDbContext).Assembly.GetName().Name;

                            sqlOptions.MigrationsAssembly(name);
                            sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                        }
                    )
                    // https://github.com/efcore/EFCore.NamingConventions
                    .UseSnakeCaseNamingConvention();

                contextBuilder?.Invoke(options);
            }
        );

        return builder;
    }
}
