namespace LeaderBoard.SharedKernel.Application.Messages.PlayerScore;

public record PlayerScoreUpdated(
    string PlayerId,
    double Score,
    string LeaderBoardName,
    long? Rank = 1
);
