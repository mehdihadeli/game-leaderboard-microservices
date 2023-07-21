namespace LeaderBoard.GameEventsProcessor.Shared.Clients.WriteThrough.Dtos;

public record IncrementScoreClientDto(double IncrementScore, string LeaderBoardName);
