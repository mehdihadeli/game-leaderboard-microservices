namespace LeaderBoard.GameEventsProcessor.Shared.Clients.ReadThrough.Dtos;

public record PlayerScoreClientDto(
    string PlayerId,
    double Score,
    string LeaderBoardName,
    long? Rank = 1,
    string? Country = null,
    string? FirstName = null,
    string? LastName = null
);
