namespace LeaderBoard.GameEventsProcessor.Shared;

public class LeaderBoardOptions
{
    public bool UseReadCacheAside { get; set; } = true;
    public bool SeedInitialData { get; set; } = true;
    public bool UseReadThrough { get; set; } = false;
    public bool UseWriteCacheAside { get; set; } = true;
    public bool UseWriteThrough { get; set; } = false;
    public bool UseWriteBehind { get; set; } = false;
    public bool CleanupRedisOnStart { get; set; } = true;
    public bool UseCacheWarmUp { get; set; } = true;
}
