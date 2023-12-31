namespace LeaderBoard.GameEventsProcessor.Shared.Clients.ReadThrough;

public class ReadThroughHttpClientOptions
{
    public string BaseAddress { get; set; } = default!;
    public int Timeout { get; set; } = 30;
    public string PlayersScoreEndpoint { get; set; } = default!;
}
