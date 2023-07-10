using LeaderBoard.SharedKernel.Data.Contracts;

namespace LeaderBoard.Models;

public class PlayerScore : IAuditable
{
    public required string PlayerId { get; set; }
    public required double Score { get; set; }
    public long? Rank { get; set; }
    public string? Country { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
