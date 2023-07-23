using LeaderBoard.SignalR.Dto;

namespace LeaderBoard.SignalR.Clients.GameEventProcessor;

public interface IGameEventProcessorClient
{
    Task<IList<PlayerScoreWithNeighborsDto>> GetPlayerGroupGlobalScoresAndRanks(
        IEnumerable<string> playerIds,
        string leaderBoardName,
        CancellationToken cancellationToken
    );
    Task<PlayerScoreWithNeighborsDto?> GetGlobalScoreAndRank(
        string playerId,
        string leaderBoardName,
        CancellationToken cancellationToken
    );
}
