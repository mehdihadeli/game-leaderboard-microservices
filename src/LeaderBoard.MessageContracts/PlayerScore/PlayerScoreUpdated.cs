namespace LeaderBoard.MessageContracts.PlayerScore;

public record PlayerScoreUpdated(
    string PlayerId,
    double Score,
    string LeaderBoardName,
    long? Rank = 1
);
