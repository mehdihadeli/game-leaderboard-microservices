namespace LeaderBoard.GameEventsProcessor.Shared.Clients.ReadThrough.Dtos;

public record PlayerScoreWithNeighborsClientDto(
    PlayerScoreClientDto? Previous,
    PlayerScoreClientDto CurrentPlayerScore,
    PlayerScoreClientDto? Next
);
