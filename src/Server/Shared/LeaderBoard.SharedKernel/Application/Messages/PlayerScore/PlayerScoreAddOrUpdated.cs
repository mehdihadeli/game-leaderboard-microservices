namespace LeaderBoard.SharedKernel.Application.Messages.PlayerScore;

public record PlayerScoreAddOrUpdated(
    string PlayerId,
    double Score,
    string LeaderBoardName,
    string FirstName,
    string LastName,
    string Country
);
