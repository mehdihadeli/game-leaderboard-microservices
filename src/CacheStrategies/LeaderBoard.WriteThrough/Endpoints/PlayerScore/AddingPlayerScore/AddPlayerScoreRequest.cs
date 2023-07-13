namespace LeaderBoard.WriteThrough.Endpoints.PlayerScore.AddingPlayerScore;

public record AddPlayerScoreRequest(
    double Score,
    string LeaderBoardName,
    string? Country = null,
    string? FirstName = null,
    string? LastName = null
);
