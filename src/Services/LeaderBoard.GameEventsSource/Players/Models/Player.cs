namespace LeaderBoard.GameEventsSource.Players.Models;

public class Player
{
    public required Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string Country { get; set; } = default!;
}
