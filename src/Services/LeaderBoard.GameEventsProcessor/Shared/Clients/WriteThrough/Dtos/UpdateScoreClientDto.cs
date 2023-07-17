namespace LeaderBoard.GameEventsProcessor.Shared.Clients.WriteThrough.Dtos;

public record UpdateScoreClientDto(double Score, string LeaderBoardName);
