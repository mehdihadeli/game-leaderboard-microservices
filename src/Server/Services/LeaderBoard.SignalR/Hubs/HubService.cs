using Microsoft.AspNetCore.SignalR;

namespace LeaderBoard.SignalR.Hubs;

// for server to client communications
//https://learn.microsoft.com/en-us/aspnet/core/signalr/hubcontext
public class HubService : IHubService
{
    private readonly IHubContext<StronglyTypedPlayerScoreHub, IPlayerScoreClient> _hubContext;

    public HubService(IHubContext<StronglyTypedPlayerScoreHub, IPlayerScoreClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public void SendHelloToClients()
    {
        _hubContext.Clients.All.HelloClient("Hello from the SignalrDemoHub!");
    }
}
