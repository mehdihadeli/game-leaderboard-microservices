namespace LeaderBoard.SharedKernel.Contracts.Domain;

public interface IBusinessRule
{
    bool IsBroken();
    string Message { get; }
}
