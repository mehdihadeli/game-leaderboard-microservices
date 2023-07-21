using LeaderBoard.WriteThrough.PlayerScore.Dtos;

namespace LeaderBoard.WriteThrough.Shared.Services;

public interface IWriteThrough
{
    Task AddOrUpdatePlayerScore(
        PlayerScoreDto playerScoreDto,
        CancellationToken cancellationToken = default
    );
}
