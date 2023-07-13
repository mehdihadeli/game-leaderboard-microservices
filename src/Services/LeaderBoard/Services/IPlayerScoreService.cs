using LeaderBoard.Dtos;
using LeaderBoard.Models;

namespace LeaderBoard.Services;

public interface IPlayerScoreService
{
    Task<bool> AddPlayerScore(
        PlayerScoreDto playerScore,
        CancellationToken cancellationToken = default
    );

    Task<bool> UpdateScore(
        string leaderBoardName,
        string playerId,
        double value,
        CancellationToken cancellationToken = default
    );

    Task<List<PlayerScoreDto>?> GetRangeScoresAndRanks(
        string leaderBoardName,
        int start,
        int end,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    );

    Task<List<PlayerScoreDto>?> GetPlayerGroupScoresAndRanks(
        string leaderBoardName,
        IEnumerable<string> playerIds,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    );

    Task<PlayerScoreDto?> GetGlobalScoreAndRank(
        string leaderBoardName,
        string playerId,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    );
}
