using LeaderBoard.GameEventsSource.Players.Models;
using LeaderBoard.GameEventsSource.Shared.Data.EFDbContext;
using LeaderBoard.SharedKernel.Core.Extensions;
using MediatR;

namespace LeaderBoard.GameEventsSource.Players.Features.CreatingPlayer;

internal record CreatePlayer(string FirstName, string LastName, string Country) : IRequest
{
    public Guid Id { get; } = Guid.NewGuid();
}

internal class CreatePlayerHandler : IRequestHandler<CreatePlayer>
{
    private readonly GameEventSourceDbContext _gameEventSourceDbContext;

    public CreatePlayerHandler(GameEventSourceDbContext gameEventSourceDbContext)
    {
        _gameEventSourceDbContext = gameEventSourceDbContext;
    }

    public async Task Handle(CreatePlayer request, CancellationToken cancellationToken)
    {
        request.NotBeNull();

        await _gameEventSourceDbContext.Players.AddAsync(
            new Player
            {
                Id = request.Id,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Country = request.Country
            },
            cancellationToken
        );

        await _gameEventSourceDbContext.SaveChangesAsync(cancellationToken);
    }
}
