namespace LeaderBoard.SharedKernel.Data.Contracts;

public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
