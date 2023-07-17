using LeaderBoard.SharedKernel.Domain.Events;

namespace LeaderBoard.SharedKernel.Application.Events;

public record PlayerScoreUpdated(
    string Id,
    double Score,
    string LeaderBoardName,
    string FirstName,
    string LastName,
    string Country
) : DomainEvent;
