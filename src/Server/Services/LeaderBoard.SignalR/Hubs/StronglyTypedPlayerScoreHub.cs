using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LeaderBoard.SignalR.Hubs;

//https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs
//https://learn.microsoft.com/en-us/aspnet/core/signalr/hubcontext
//https://mfcallahan.blog/2020/11/05/how-to-implement-signalr-in-a-net-core-angular-web-application/
//https://kristoffer-strube.dk/post/typed-signalr-clients-making-type-safe-real-time-communication-in-dotnet/
//https://code-maze.com/how-to-send-client-specific-messages-using-signalr/

// use for calling hub endpoint from client to server communication
[Authorize]
public class StronglyTypedPlayerScoreHub : Hub<IPlayerScoreClient>
{
    public StronglyTypedPlayerScoreHub() { }

    public static readonly Dictionary<string, string> ConnectedClients =
        new Dictionary<string, string>();

    public async Task Login() { }

    [HubMethodName("HelloServer")]
    public void HelloServer()
    {
        Clients.Caller.HelloClient("Hello from the HelloServer!");
    }

    [HubMethodName("HelloServerWithConnection")]
    public void HelloServerWithConnection(string connectionId)
    {
        Clients.Client(connectionId).HelloClient($"Hello Connection: {connectionId}");
    }

    [HubMethodName("GetConnectionId")]
    public string GetConnectionId() => Context.ConnectionId;

    public override Task OnConnectedAsync()
    {
        string connectionId = Context.ConnectionId;
        return base.OnConnectedAsync();
    }
}
