using LeaderBoard.SharedKernel.Contracts.Data.EventStore.Projections;
using LeaderBoard.SharedKernel.Contracts.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LeaderBoard.SharedKernel.Core.Data.Ef.Projections;

public static class Extensions
{
    public static IServiceCollection AddEfCoreProject<TContext, TEvent, TView>(
        this IServiceCollection services,
        Func<TEvent, Guid> getId
    )
        where TView : class, IVersionedProjection
        where TEvent : IDomainEvent
        where TContext : DbContext
    {
        services.AddTransient<IReadProjection<TEvent>>(sp =>
        {
            var context = sp.GetRequiredService<TContext>();

            return new EfProjectionBase<TContext, TEvent, TView>(context, getId);
        });

        return services;
    }
}
