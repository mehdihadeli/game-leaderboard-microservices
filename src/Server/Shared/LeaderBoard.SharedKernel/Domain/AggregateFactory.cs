using System.Linq.Expressions;

namespace LeaderBoard.SharedKernel.Domain;

public static class AggregateFactory<T>
{
    private static readonly Func<T> Constructor = CreateTypeConstructor();

    private static Func<T> CreateTypeConstructor()
    {
        try
        {
            var newExpr = Expression.New(typeof(T));
            var func = Expression.Lambda<Func<T>>(newExpr);
            return func.Compile();
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public static T CreateAggregate()
    {
        if (Constructor == null)
            throw new Exception($"Aggregate {typeof(T).Name} does not have a parameterless constructor");
        return Constructor();
    }
}
