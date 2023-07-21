using Humanizer;
using LeaderBoard.GameEventsSource.Players.Models;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LeaderBoard.GameEventsSource.Players.Features.CreatingPlayer;

internal static class CreatePlayerEndpoint
{
    internal static RouteHandlerBuilder MapCreatePlayerEndpoint(
        this IEndpointRouteBuilder routeBuilder
    )
    {
        return routeBuilder
            .MapPost("/", Handle)
            .WithTags(nameof(Player).Pluralize())
            .WithName(nameof(CreatePlayer));

        static async Task<Results<NoContent, BadRequest>> Handle(
            [AsParameters] CreatePlayerRequestParameters requestParameters
        )
        {
            var (req, mediator, ct) = requestParameters;

            await mediator.Send(
                new CreatePlayer(
                    req.Email,
                    req.UserName,
                    req.Password,
                    req.FirstName,
                    req.LastName,
                    req.Country
                ),
                ct
            );

            return TypedResults.NoContent();
        }
    }

    internal record CreatePlayerRequestParameters(
        CreatePlayerRequest Request,
        IMediator Mediator,
        CancellationToken CancellationToken
    );

    internal record CreatePlayerRequest(
        string Email,
        string Password,
        string UserName,
        string FirstName,
        string LastName,
        string Country
    );
}
