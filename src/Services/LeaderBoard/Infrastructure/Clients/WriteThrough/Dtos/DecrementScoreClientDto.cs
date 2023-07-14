namespace LeaderBoard.Infrastructure.Clients.WriteThrough.Dtos;

public record DecrementScoreClientDto(double DecrementScore, string LeaderBoardName);
