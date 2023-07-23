namespace LeaderBoard.SignalR.Dto;

public record PlayerScoreWithNeighborsDto(
    PlayerScoreDto? Previous,
    PlayerScoreDto CurrentPlayerScore,
    PlayerScoreDto? Next
);
