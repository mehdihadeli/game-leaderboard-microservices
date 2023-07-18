namespace LeaderBoard.GameEventsProcessor.PlayerScores.Dtos;

public record PlayerScoreWithNeighborsDto(
    PlayerScoreDto? Previous,
    PlayerScoreDto CurrentPlayerScore,
    PlayerScoreDto? Next
);
