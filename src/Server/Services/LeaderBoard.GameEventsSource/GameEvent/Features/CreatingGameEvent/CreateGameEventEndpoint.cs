using Humanizer;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace LeaderBoard.GameEventsSource.GameEvent.Features.CreatingGameEvent;

internal static class CreateGameEventEndpoint
{
    internal static RouteHandlerBuilder MapCreateGameEventEndpoint(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder
            .MapPost("players/{playerId:guid}", Handle)
            .WithTags(nameof(GameEvent).Pluralize())
            .WithName("CreateGameEvents");

        static async Task<Results<NoContent, BadRequest>> Handle(
            [AsParameters] CreateGameEventRequestParameters requestParameters
        )
        {
            var (playerId, req, mediator, ct) = requestParameters;

            await mediator.Send(new CreateGameEvent(playerId, req.Score, req.FirstName, req.LastName, req.Country), ct);

            return TypedResults.NoContent();
        }
    }

    internal record CreateGameEventRequestParameters(
        [FromRoute] Guid PlayerId,
        CreateGameEventRequest Request,
        IMediator Mediator,
        CancellationToken CancellationToken
    );

    internal record CreateGameEventRequest(double Score, string FirstName, string LastName, string Country);
}
