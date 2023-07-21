namespace LeaderBoard.GameEventsProcessor.Shared.Clients.WriteThrough.Dtos;

public record DecrementScoreClientDto(double DecrementScore, string LeaderBoardName);
