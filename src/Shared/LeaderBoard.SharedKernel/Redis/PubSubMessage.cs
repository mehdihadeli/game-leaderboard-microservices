namespace LeaderBoard.SharedKernel.Redis;

public class PubSubMessage
{
    public string Type { get; set; } = default!;
    public string Data { get; set; } = default!;
    public string ServerId { get; set; } = "123";
}
