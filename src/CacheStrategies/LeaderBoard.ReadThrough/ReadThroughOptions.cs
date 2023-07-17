namespace LeaderBoard.ReadThrough;

public class ReadThroughOptions
{
    public bool UseCacheWarmUp { get; set; } = true;
    public bool CleanupRedisOnStart { get; set; } = true;
}
