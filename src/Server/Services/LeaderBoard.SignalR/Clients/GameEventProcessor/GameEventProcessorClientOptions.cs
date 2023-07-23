namespace LeaderBoard.SignalR.Clients.GameEventProcessor;

public class GameEventProcessorClientOptions
{
    public string BaseAddress { get; set; } = default!;
    public int Timeout { get; set; } = 30;
    public string PlayersScoreEndpoint { get; set; } = default!;
}
