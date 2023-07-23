namespace LeaderBoard.WriteBehind.Shared.Dtos;

public record PlayerScoreDto(
    string PlayerId,
    double Score,
    string LeaderBoardName,
    string FirstName,
    string LastName,
    string Country
);
