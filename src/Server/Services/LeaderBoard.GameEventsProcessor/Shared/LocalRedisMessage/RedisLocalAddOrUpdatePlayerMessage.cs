using Humanizer;

namespace LeaderBoard.GameEventsProcessor.Shared.LocalRedisMessage;

public record RedisLocalAddOrUpdatePlayerMessage(
    string PlayerId,
    double Score,
    string LeaderBoardName,
    string FirstName,
    string LastName,
    string Country
)
{
    public static string ChannelName { get; } =
        $"{nameof(GameEventsProcessor).Underscore()}_{nameof(RedisLocalAddOrUpdatePlayerMessage).Underscore()}_channel";
};
