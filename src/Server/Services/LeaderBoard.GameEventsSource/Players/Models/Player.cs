using Microsoft.AspNetCore.Identity;

namespace LeaderBoard.GameEventsSource.Players.Models;

public class Player : IdentityUser<Guid>
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Country { get; set; } = default!;
}
