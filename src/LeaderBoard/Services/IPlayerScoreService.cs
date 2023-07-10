using LeaderBoard.Dtos;

namespace LeaderBoard.Services;

public interface IPlayerScoreService
{
    Task<bool> AddOrUpdateScore(string leaderBoardName, string playerId, double value);

    /// <summary>
    /// Get details information about saved sortedset player score
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    Task<PlayerScoreDetailDto> GetPlayerScoreDetail(string playerId);

    Task<List<PlayerScoreDto>> GetScoresAndRanks(
        string leaderBoardName,
        int start,
        int ent,
        bool isDesc = true
    );

    Task<PlayerScoreDto> GetScoreAndRank(
        string leaderBoardName,
        string playerId,
        bool isDesc = true
    );
}
