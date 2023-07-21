namespace LeaderBoard.SharedKernel.Contracts.Domain;

public interface IHaveAudit : IHaveCreator
{
    DateTime? LastModified { get; }
    int? LastModifiedBy { get; }
}
