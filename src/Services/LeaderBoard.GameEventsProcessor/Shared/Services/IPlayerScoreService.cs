using LeaderBoard.GameEventsProcessor.PlayerScores.Dtos;
using LeaderBoard.SharedKernel.Application.Models;
using StackExchange.Redis;

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
    RedisValue GetNextMember(string leaderBoardName, string memberKey, bool isDesc = true);
    RedisValue GetPreviousMember(string leaderBoardName, string memberKey, bool isDesc = true);
}
