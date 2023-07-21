namespace LeaderBoard.SharedKernel.Contracts.Data;

public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
