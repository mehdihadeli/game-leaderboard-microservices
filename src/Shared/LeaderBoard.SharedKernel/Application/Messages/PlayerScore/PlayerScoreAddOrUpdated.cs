namespace LeaderBoard.SharedKernel.Application.Messages.PlayerScore;

public record PlayerScoreAddOrUpdated(
    string PlayerId,
    double Score,
    string LeaderBoardName,
    string Country,
    string FirstName,
    string LastName
);
