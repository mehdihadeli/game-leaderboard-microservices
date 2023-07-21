using LeaderBoard.ReadThrough.PlayerScores.Dtos;

namespace LeaderBoard.ReadThrough.Shared.Services;

public interface IReadThrough
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

    Task<PlayerScoreDto?> GetNextMember(
        string leaderBoardName,
        string memberKey,
        bool isDesc = true
    );

    Task<PlayerScoreDto?> GetNextMemberByRank(
        string leaderBoardName,
        long rank,
        bool isDesc = true
    );
    Task<PlayerScoreDto?> GetPreviousMember(
        string leaderBoardName,
        string memberKey,
        bool isDesc = true
    );
    Task<PlayerScoreDto?> GetPreviousMemberByRank(
        string leaderBoardName,
        long rank,
        bool isDesc = true
    );
}
