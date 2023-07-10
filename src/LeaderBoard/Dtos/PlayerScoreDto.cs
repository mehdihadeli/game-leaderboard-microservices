namespace LeaderBoard.Dtos;

public record PlayerScoreDto(
    string PlayerId,
    double Score,
    long? Rank = 1,
    string? Country = null,
    string? FirstName = null,
    string? LastName = null
);
