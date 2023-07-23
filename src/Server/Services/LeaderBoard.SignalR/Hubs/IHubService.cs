namespace LeaderBoard.SignalR.Hubs;

public interface IHubService
{
    void SendHelloToClients();
    Task UpdatePlayersScoreForClient(IEnumerable<string> players);
}
