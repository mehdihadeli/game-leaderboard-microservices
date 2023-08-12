using System.Reflection;
using EventStore.Client;
using LeaderBoard.SharedKernel.Core.Extensions.ServiceCollectionExtensions;
using LeaderBoard.SharedKernel.Domain.Events;
using LeaderBoard.SharedKernel.EventStoreDB.Subscriptions;
using LeaderBoard.SharedKernel.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;

namespace LeaderBoard.SharedKernel.EventStoreDB.Extensions;

public static class RegistrationExtensions
{
    public static IServiceCollection AddEventStoreDB(
        this IServiceCollection services,
        AsyncPolicy? asyncPolicy = null,
        params Assembly[] scanAssemblies
    )
    {
        var assembliesToScan = scanAssemblies.Any()
            ? scanAssemblies
            : new[] { Assembly.GetCallingAssembly(), };

        services.AddValidatedOptions<EventStoreDBOptions>();

        services.AddEventSourcing<EventStoreDBEventStore>(assembliesToScan, asyncPolicy);

        services
            .AddSingleton(EventTypeMapper.Instance)
            .AddSingleton(sp =>
            {
                var option = sp.GetRequiredService<IOptions<EventStoreDBOptions>>().Value;
                return new EventStoreClient(
                    EventStoreClientSettings.Create(option.GrpcConnectionString)
                );
            });

        services.AddTransient<
            ISubscriptionCheckpointRepository,
            EventStoreDBSubscriptionCheckpointRepository
        >();

        services.AddEventStoreDbSubscriptionToAll();

        return services;
    }

    private static IServiceCollection AddEventStoreDbSubscriptionToAll(
        this IServiceCollection services,
        bool checkpointToEventStoreDb = true
    )
    {
        services.AddValidatedOptions<EventStoreDBSubscriptionToAllOptions>();
        if (checkpointToEventStoreDb)
        {
            services.AddTransient<
                ISubscriptionCheckpointRepository,
                EventStoreDBSubscriptionCheckpointRepository
            >();
        }

        TelemetryPropagator.UseDefaultCompositeTextMapPropagator();

        services.AddHostedService<EventStoreDBSubscriptionToAll>();

        return services;
    }
}
