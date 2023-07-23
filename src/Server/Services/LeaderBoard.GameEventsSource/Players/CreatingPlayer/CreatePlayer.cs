using LeaderBoard.GameEventsSource.Players.Models;
using LeaderBoard.SharedKernel.Core.Exceptions;
using LeaderBoard.SharedKernel.Core.Extensions;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace LeaderBoard.GameEventsSource.Players.CreatingPlayer;

internal record CreatePlayer(
    string Email,
    string UserName,
    string Password,
    string FirstName,
    string LastName,
    string Country
) : IRequest
{
    public Guid Id { get; } = Guid.NewGuid();
}

internal class CreatePlayerHandler : IRequestHandler<CreatePlayer>
{
    private readonly UserManager<Player> _userManager;

    public CreatePlayerHandler(UserManager<Player> userManager)
    {
        _userManager = userManager;
    }

    public async Task Handle(CreatePlayer request, CancellationToken cancellationToken)
    {
        request.NotBeNull();

        var player = new Player
        {
            Id = request.Id,
            Email = request.Email,
            UserName = request.UserName,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Country = request.Country
        };

        IdentityResult identityResult = await _userManager.CreateAsync(player, request.Password);

        if (identityResult.Succeeded == false)
        {
            throw new ValidationException(
                string.Join(',', identityResult.Errors.Select(x => x.Description))
            );
        }
    }
}
