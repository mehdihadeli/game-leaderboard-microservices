namespace LeaderBoard.GameEventsSource;

public class GameEventSourceOptions
{
    public int PublishDelay { get; set; } = 10;
    public bool EnablePublishWorker { get; set; } = false;
}
