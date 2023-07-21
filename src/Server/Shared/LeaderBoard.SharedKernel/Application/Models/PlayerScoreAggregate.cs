using LeaderBoard.SharedKernel.Application.Events;
using LeaderBoard.SharedKernel.Domain.EventSourcing;

namespace LeaderBoard.SharedKernel.Application.Models;

public class PlayerScoreAggregate : EventSourcedAggregate<string>
{
    public double Score { get; private set; }
    public string LeaderBoardName { get; private set; } = default!;
    public string Country { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;

    public static PlayerScoreAggregate Create(
        string id,
        double score,
        string leaderBoardName,
        string firstName,
        string lastName,
        string country
    )
    {
        var playerScore = new PlayerScoreAggregate();
        var @event = new PlayerScoreAdded(id, score, leaderBoardName, firstName, lastName, country);

        playerScore.ApplyEvent(@event);

        return playerScore;
    }

    public PlayerScoreAggregate Update(
        double score,
        string firstName,
        string lastName,
        string country
    )
    {
        var @event = new PlayerScoreUpdated(
            Id,
            score,
            LeaderBoardName,
            firstName,
            lastName,
            country
        );

        ApplyEvent(@event);

        return this;
    }

    private void Apply(PlayerScoreAdded @event)
    {
        Id = @event.Id;
        Country = @event.Country;
        FirstName = @event.FirstName;
        LastName = @event.LastName;
        LeaderBoardName = @event.LeaderBoardName;
        Score += @event.Score;
    }

    private void Apply(PlayerScoreUpdated @event)
    {
        Country = @event.Country;
        FirstName = @event.FirstName;
        LastName = @event.LastName;
        Score += @event.Score;
    }
}
