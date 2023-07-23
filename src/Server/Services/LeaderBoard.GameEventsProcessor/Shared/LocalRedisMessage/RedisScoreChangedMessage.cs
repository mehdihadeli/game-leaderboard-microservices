using Humanizer;

namespace LeaderBoard.GameEventsProcessor.Shared.LocalRedisMessage;

public record RedisScoreChangedMessage(
    string PlayerId,
    string LeaderBoardName,
    double PreviousScore,
    double UpdatedScore,
    bool IsDesc = true
)
{
    public static string ChannelName { get; } =
        $"{nameof(GameEventsProcessor).Underscore()}_{nameof(RedisScoreChangedMessage).Underscore()}_channel";
}
