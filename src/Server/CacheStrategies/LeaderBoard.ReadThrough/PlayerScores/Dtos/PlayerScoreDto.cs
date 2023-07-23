namespace LeaderBoard.ReadThrough.PlayerScores.Dtos;

public record PlayerScoreDto(
    string PlayerId,
    double Score,
    string LeaderBoardName,
    long? Rank = 1,
    string? FirstName = null,
    string? LastName = null,
    string? Country = null
);
