using LeaderBoard.SignalR.Clients.GameEventProcessor;
using Microsoft.AspNetCore.SignalR;

namespace LeaderBoard.SignalR.Hubs;

// for server to client communications
//https://learn.microsoft.com/en-us/aspnet/core/signalr/hubcontext
public class HubService : IHubService
{
    private readonly IHubContext<StronglyTypedPlayerScoreHub, IPlayerScoreClient> _hubContext;
    private readonly IGameEventProcessorClient _gameEventProcessorClient;

    public HubService(
        IHubContext<StronglyTypedPlayerScoreHub, IPlayerScoreClient> hubContext,
        IGameEventProcessorClient gameEventProcessorClient
    )
    {
        _hubContext = hubContext;
        _gameEventProcessorClient = gameEventProcessorClient;
    }

    public void SendHelloToClients()
    {
        _hubContext.Clients.All.HelloClient("Hello from the Server SendHelloToClients!");
    }

    public async Task UpdatePlayersScoreForClient(IEnumerable<string> players)
    {
        var playersList = players.ToList();

        var connectedUsers = playersList.Where(
            playerId => StronglyTypedPlayerScoreHub.UsersConnections.ContainsKey(playerId)
        );

        var playersScore = await _gameEventProcessorClient.GetPlayerGroupGlobalScoresAndRanks(
            connectedUsers,
            Constants.GlobalLeaderBoard,
            CancellationToken.None
        );

        foreach (var ps in playersScore)
        {
            await _hubContext.Clients.User(ps.CurrentPlayerScore.PlayerId).UpdatePlayerScoreForClient(ps);
        }
    }
}
