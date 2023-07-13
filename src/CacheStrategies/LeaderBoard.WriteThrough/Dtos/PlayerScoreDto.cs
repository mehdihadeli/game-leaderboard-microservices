namespace LeaderBoard.WriteThrough.Dtos;

public record PlayerScoreDto(
    string PlayerId,
    double Score,
    string LeaderBoardName,
    long? Rank = 1,
    string? Country = null,
    string? FirstName = null,
    string? LastName = null
);
