namespace LeaderBoard.WriteBehind;

public class ReadThroughOptions
{
    public bool UseCacheWarmUp { get; set; } = true;
    public bool CleanupRedisOnStart { get; set; } = true;
}
