using LeaderBoard.GameEventsProcessor.PlayerScores.Dtos;

namespace LeaderBoard.GameEventsProcessor.Shared.Clients.ReadThrough;

public interface IReadThroughClient
{
    Task<List<PlayerScoreDto>?> GetRangeScoresAndRanks(
        string leaderBoardName,
        int start,
        int end,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    );
    Task<PlayerScoreWithNeighborsDto?> GetGlobalScoreAndRank(
        string leaderBoardName,
        string playerId,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    );
    Task<List<PlayerScoreWithNeighborsDto>?> GetPlayerGroupGlobalScoresAndRanks(
        string leaderBoardName,
        IEnumerable<string> playerIds,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    );
}
