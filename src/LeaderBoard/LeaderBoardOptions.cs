namespace LeaderBoard;

public class LeaderBoardOptions
{
    public bool UseReadCacheAside { get; set; }
    public bool UseWriteCacheAside { get; set; }
    public bool UseReadThrough { get; set; }
    public bool UseWriteBehind { get; set; }
}
