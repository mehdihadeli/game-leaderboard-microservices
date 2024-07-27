namespace LeaderBoard.SharedKernel.Application.Messages;

public record GameEventChanged(string PlayerId, double Score, string FirstName, string LastName, string Country);
