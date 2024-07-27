using LeaderBoard.GameEventsProcessor.PlayerScores.Dtos;

namespace LeaderBoard.GameEventsProcessor.Shared.Clients.WriteThrough;

public interface IWriteThroughClient
{
    Task AddOrUpdatePlayerScore(PlayerScoreDto playerScoreDto, CancellationToken cancellationToken = default);
}
