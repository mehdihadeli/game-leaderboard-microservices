namespace LeaderBoard.SignalR.Clients.GameEventProcessor.Dtos;

public record PlayerScoreClientDto(
    string PlayerId,
    double Score,
    string LeaderBoardName,
    long? Rank,
    string FirstName,
    string LastName,
    string Country
);
