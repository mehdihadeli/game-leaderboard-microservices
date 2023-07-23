using LeaderBoard.SignalR.Clients.GameEventProcessor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LeaderBoard.SignalR.Hubs;

//https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs
//https://learn.microsoft.com/en-us/aspnet/core/signalr/hubcontext
//https://mfcallahan.blog/2020/11/05/how-to-implement-signalr-in-a-net-core-angular-web-application/
//https://kristoffer-strube.dk/post/typed-signalr-clients-making-type-safe-real-time-communication-in-dotnet/
//https://code-maze.com/how-to-send-client-specific-messages-using-signalr/
//https://referbruv.com/blog/how-to-use-signalr-with-asp-net-core-angular/

// use for calling hub endpoint from client to server communication
[Authorize]
public class StronglyTypedPlayerScoreHub : Hub<IPlayerScoreClient>
{
    private readonly IGameEventProcessorClient _gameEventProcessorClient;
    public static readonly Dictionary<string, List<string>> UsersConnections = new();
    public static readonly IList<string> ConnectedConnectionIds = new List<string>();

    public StronglyTypedPlayerScoreHub(IGameEventProcessorClient gameEventProcessorClient)
    {
        _gameEventProcessorClient = gameEventProcessorClient;
    }

    [HubMethodName("HelloFromServer")]
    public void HelloFromServer()
    {
        Clients.Caller.HelloClient("Hello from the HelloServer!");
    }

    [HubMethodName("HelloWithConnectionFromServer")]
    public void HelloWithConnectionFromServer(string connectionId)
    {
        Clients.Client(connectionId).HelloClient($"Hello Connection: {connectionId}");
    }

    public async Task GetCurrentPlayerScoreFromServer()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return;

        var playerScore = await _gameEventProcessorClient.GetGlobalScoreAndRank(
            userId,
            Constants.GlobalLeaderBoard,
            CancellationToken.None
        );

        if (playerScore is not null)
        {
            await Clients.Caller.InitialPlayerScoreForClient(playerScore);
        }
    }

    [HubMethodName("GetConnectionId")]
    public string GetConnectionId() => Context.ConnectionId;

    public override Task OnConnectedAsync()
    {
        string connectionId = Context.ConnectionId;
        string? userId = GetUserId();

        ConnectedConnectionIds.Add(connectionId);

        if (!string.IsNullOrEmpty(userId))
        {
            if (!UsersConnections.ContainsKey(userId))
            {
                UsersConnections.Add(userId, new List<string> { connectionId });
            }
            else
            {
                UsersConnections[userId].Add(connectionId);
            }
        }

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        string connectionId = Context.ConnectionId;
        string? userId = GetUserId();

        ConnectedConnectionIds.Remove(connectionId);

        if (!string.IsNullOrEmpty(userId) && UsersConnections.ContainsKey(userId))
        {
            UsersConnections[userId].Remove(connectionId);

            if (UsersConnections[userId].Count == 0)
            {
                UsersConnections.Remove(userId);
            }
        }

        return base.OnDisconnectedAsync(exception);
    }

    private string? GetUserId()
    {
        return Context.UserIdentifier;
    }
}
