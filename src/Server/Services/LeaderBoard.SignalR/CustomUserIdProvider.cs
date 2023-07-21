using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LeaderBoard.SignalR;

//https://stackoverflow.com/questions/19522103/signalr-sending-a-message-to-a-specific-user-using-iuseridprovider-new-2-0
public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var userId = connection.User?.Claims
            .SingleOrDefault(x => x.Type == JwtRegisteredClaimNames.NameId)
            ?.Value;

        return userId;
    }
}
