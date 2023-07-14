namespace LeaderBoard.Infrastructure.Clients.WriteThrough.Dtos;

public record IncrementScoreClientDto(double IncrementScore, string LeaderBoardName);
