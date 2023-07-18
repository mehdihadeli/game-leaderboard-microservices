namespace LeaderBoard.ReadThrough.PlayerScores.Dtos;

public record PlayerScoreWithNeighborsDto(
    PlayerScoreDto? Previous,
    PlayerScoreDto CurrentPlayerScore,
    PlayerScoreDto? Next
);
