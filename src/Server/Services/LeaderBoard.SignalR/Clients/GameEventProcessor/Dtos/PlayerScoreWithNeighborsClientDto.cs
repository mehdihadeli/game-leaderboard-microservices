namespace LeaderBoard.SignalR.Clients.GameEventProcessor.Dtos;

public record PlayerScoreWithNeighborsClientDto(
    PlayerScoreClientDto? Previous,
    PlayerScoreClientDto CurrentPlayerScore,
    PlayerScoreClientDto? Next
);
