using LeaderBoard.SharedKernel.Contracts.Domain;

namespace LeaderBoard.SharedKernel.EventStoreDB.Repository;

public interface IEventStoreDBRepository<T> where T : class, IAggregate
{
	Task<T?> Find(Guid id, CancellationToken cancellationToken);
	Task<ulong> Add(T aggregate, CancellationToken ct = default);
	Task<ulong> Update(T aggregate, ulong? expectedRevision = null, CancellationToken ct = default);
	Task<ulong> Delete(T aggregate, ulong? expectedRevision = null, CancellationToken ct = default);
}