namespace LeaderBoard.SharedKernel.Contracts.Domain;

public interface IHaveCreator
{
    DateTime Created { get; }
    int? CreatedBy { get; }
}
