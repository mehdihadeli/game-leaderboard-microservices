namespace LeaderBoard.Infrastructure.Clients.WriteThrough.Dtos;

public record AddPlayerScoreClientDto(
    double Score,
    string LeaderBoardName,
    string? Country = null,
    string? FirstName = null,
    string? LastName = null
);
