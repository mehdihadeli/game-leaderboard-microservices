using LeaderBoard.Dtos;

namespace LeaderBoard.Infrastructure.Clients.WriteThrough;

public interface IWriteThroughClient
{
    Task AddPlayerScore(
        PlayerScoreDto playerScoreDto,
        CancellationToken cancellationToken = default
    );

    Task UpdateScore(
        string playerId,
        double score,
        string leaderBoardName,
        CancellationToken cancellationToken = default
    );

    Task DecrementScore(
        string leaderBoardName,
        string playerId,
        double decrementScore,
        CancellationToken cancellationToken = default
    );

    Task IncrementScore(
        string leaderBoardName,
        string playerId,
        double incrementScore,
        CancellationToken cancellationToken = default
    );
}
