using LeaderBoard.GameEventsSource.Players.Models;
using LeaderBoard.SharedKernel.Core.Extensions;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace LeaderBoard.GameEventsSource.Accounts.GettingProfile;

public record GetProfile(string? UserId) : IRequest<GetProfileResult?>;

internal class GetProfileHandler : IRequestHandler<GetProfile, GetProfileResult?>
{
    private readonly UserManager<Player> _userManager;

    public GetProfileHandler(UserManager<Player> userManager)
    {
        _userManager = userManager;
    }

    public async Task<GetProfileResult?> Handle(
        GetProfile request,
        CancellationToken cancellationToken
    )
    {
        request.NotBeNull();
        request.UserId.NotBeEmptyOrNull();

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return null;
        }

        return new GetProfileResult(
            user.Id.ToString(),
            user.FirstName,
            user.LastName,
            user.Country,
            user.Email,
            user.UserName
        );
    }
}

record GetProfileResult(
    string Id,
    string FirstName,
    string LastName,
    string Country,
    string? Email,
    string? UserName
);
