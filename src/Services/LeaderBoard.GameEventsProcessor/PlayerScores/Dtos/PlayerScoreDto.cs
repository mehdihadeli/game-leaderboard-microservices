namespace LeaderBoard.GameEventsProcessor.PlayerScores.Dtos;

public record PlayerScoreDto(
    string PlayerId,
    double Score,
    string LeaderBoardName,
    long? Rank,
    string FirstName,
    string LastName,
    string Country
);
