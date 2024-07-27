using System.Security.Claims;
using LeaderBoard.GameEventsSource.Players.Models;

namespace LeaderBoard.GameEventsSource.Shared.Services;

public interface ITokenService
{
    Task<string> GetJwtTokenAsync(Player user);
    ClaimsPrincipal ParseExpiredToken(string accessToken);
}
