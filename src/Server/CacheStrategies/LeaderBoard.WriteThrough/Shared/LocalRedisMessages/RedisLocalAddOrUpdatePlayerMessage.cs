using Humanizer;

namespace LeaderBoard.WriteThrough.Shared.LocalRedisMessages;

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
        $"{nameof(WriteThrough).Underscore()}_{nameof(RedisLocalAddOrUpdatePlayerMessage).Underscore()}_channel";
};
