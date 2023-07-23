using Humanizer;

namespace LeaderBoard.WriteThrough.Shared.LocalRedisMessages;

public record RedisScoreChangedMessage(
    string PlayerId,
    string LeaderBoardName,
    double PreviousScore,
    double UpdatedScore,
    bool IsDesc = true
)
{
    public static string ChannelName { get; } =
        $"{nameof(WriteThrough).Underscore()}_{nameof(RedisScoreChangedMessage).Underscore()}_channel";
};
