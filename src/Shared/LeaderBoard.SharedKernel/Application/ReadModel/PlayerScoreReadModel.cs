using LeaderBoard.SharedKernel.Application.Events;
using LeaderBoard.SharedKernel.Contracts.Data;
using LeaderBoard.SharedKernel.Contracts.Data.EventStore.Projections;

namespace LeaderBoard.SharedKernel.Application.Models;

public class PlayerScoreReadModel : IAuditable, IVersionedProjection
{
    public string PlayerId { get; set; } = default!;
    public double Score { get; set; }
    public string LeaderBoardName { get; set; } = default!;
    public string? Country { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ulong LastProcessedPosition { get; set; }

    public void When(object @event)
    {
        switch (@event)
        {
            case PlayerScoreAdded playerScoreAdded:
                Apply(playerScoreAdded);
                break;

            case PlayerScoreUpdated playerScoreUpdated:
                Apply(playerScoreUpdated);
                break;
        }
    }

    public void Apply(PlayerScoreAdded @event)
    {
        Score += @event.Score;
        PlayerId = @event.Id;
        LeaderBoardName = @event.LeaderBoardName;
        FirstName = @event.FirstName;
        LastName = @event.LastName;
        Country = @event.Country;
    }

    public void Apply(PlayerScoreUpdated @event)
    {
        Score += @event.Score;
        FirstName = @event.FirstName;
        LastName = @event.LastName;
        Country = @event.Country;
    }
}
