using LeaderBoard.GameEventsProcessor.PlayerScores.Dtos;
using LeaderBoard.SharedKernel.Application.Models;

namespace LeaderBoard.GameEventsProcessor.Shared.Services;

public interface IPlayerScoreService
{
    Task PopulateCache(PlayerScoreReadModel playerScore);
    Task PopulateCache(IQueryable<PlayerScoreReadModel> databaseQuery);

    Task<PlayerScoreDetailDto?> GetPlayerScoreDetail(
        string leaderboardName,
        string playerIdKey,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    );
    Task<PlayerScoreDto?> GetNextMember(string leaderBoardName, string memberKey, bool isDesc = true);

    Task<PlayerScoreDto?> GetNextMemberByRank(string leaderBoardName, long rank, bool isDesc = true);
    Task<PlayerScoreDto?> GetPreviousMember(string leaderBoardName, string memberKey, bool isDesc = true);
    Task<PlayerScoreDto?> GetPreviousMemberByRank(string leaderBoardName, long rank, bool isDesc = true);
}
