namespace LeaderBoard.WriteBehind.Dtos;

public record PlayerScoreDto(
    string PlayerId,
    double Score,
    string LeaderBoardName,
    string FirstName,
    string LastName,
    string Country
);
