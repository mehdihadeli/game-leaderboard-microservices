using System.Reflection;
using LeaderBoard.SharedKernel.Contracts.Data.EventStore;
using LeaderBoard.SharedKernel.Contracts.Data.EventStore.Projections;
using LeaderBoard.SharedKernel.Contracts.Domain.Events;
using LeaderBoard.SharedKernel.Core.Data.EventStore;
using LeaderBoard.SharedKernel.Core.Data.EventStore.InMemory;
using LeaderBoard.SharedKernel.Core.Projections;
using LeaderBoard.SharedKernel.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace LeaderBoard.SharedKernel.Core.Extensions.ServiceCollectionExtensions;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddInMemoryEventStore(
        this IServiceCollection services,
        AsyncPolicy? asyncPolicy = null,
        params Assembly[] scanAssemblies
    )
    {
        var assembliesToScan = scanAssemblies.Any()
            ? scanAssemblies
            : new[] { Assembly.GetCallingAssembly(), };

        return services.AddEventSourcing<InMemoryEventStore>(assembliesToScan, asyncPolicy);
    }

    public static IServiceCollection AddEventSourcing<TEventStore>(
        this IServiceCollection services,
        Assembly[] scanAssemblies,
        AsyncPolicy? asyncPolicy = null
    )
        where TEventStore : class, IEventStore
    {
        services.AddScoped<IAggregateStore, AggregateStore>();
        services.AddScoped<IDomainEventsAccessor, EventStoreDomainEventAccessor>();

        services
            .AddScoped<TEventStore, TEventStore>()
            .AddScoped<IEventStore>(sp => sp.GetRequiredService<TEventStore>());

        services.AddProjections(asyncPolicy, scanAssemblies);

        return services;
    }

    private static IServiceCollection AddProjections(
        this IServiceCollection services,
        AsyncPolicy? asyncPolicy = null,
        params Assembly[] assemblies
    )
    {
        services.AddSingleton<IReadProjectionPublisher, ReadProjectionPublisher>();

        services.AddSingleton<IProjectionPublisher, ProjectionPublisher>(
            sp =>
                new ProjectionPublisher(
                    sp,
                    sp.GetRequiredService<IActivityScope>(),
                    asyncPolicy ?? Policy.NoOpAsync()
                )
        );
        var assembliesToScan = assemblies.Any()
            ? assemblies
            : new[] { Assembly.GetEntryAssembly() };

        RegisterReadProjections(services, assembliesToScan!);

        return services;
    }

    private static void RegisterReadProjections(
        this IServiceCollection services,
        params Assembly[] assembliesToScan
    )
    {
        services.Scan(
            scan =>
                scan.FromAssemblies(assembliesToScan)
                    .AddClasses(classes => classes.AssignableTo<IReadProjection>()) // Filter classes
                    .AsImplementedInterfaces()
                    .WithTransientLifetime()
        );

        services.Scan(
            scan =>
                scan.FromAssemblies(assembliesToScan)
                    .AddClasses(classes => classes.AssignableTo<IHaveReadProjection>()) // Filter classes
                    .AsImplementedInterfaces()
                    .WithTransientLifetime()
        );
    }
}
