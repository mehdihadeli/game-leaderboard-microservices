namespace LeaderBoard.WriteBehind.Shared;

public class WriteBehindOptions
{
    public bool UseRedisStreamWriteBehind { get; set; }
    public bool UseRedisPubSubWriteBehind { get; set; }
    public bool UseBrokerWriteBehind { get; set; }
}
