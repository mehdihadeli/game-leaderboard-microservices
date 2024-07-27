using LeaderBoard.GameEventsProcessor.Shared;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace LeaderBoard.GameEventsProcessor.PlayerScores.Features.AddingOrUpdatingPlayerScore;

internal static class AddOrUpdatePlayerScoreEndpoint
{
    internal static RouteHandlerBuilder MapAddOrUpdatePlayerScoreEndpoint(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder
            .MapPost("players/{playerId:guid}", Handle)
            .WithTags(nameof(PlayerScores))
            .WithName("AddPlayerScore");

        static async Task<Results<NoContent, BadRequest>> Handle(
            [AsParameters] AddPlayerScoreRequestParameters requestParameters
        )
        {
            var (playerId, req, mediator, ct) = requestParameters;

            await mediator.Send(
                new AddOrUpdatePlayerScore(
                    playerId,
                    req.Score,
                    req.LeaderBoardName,
                    req.FirstName,
                    req.LastName,
                    req.Country
                ),
                ct
            );

            return TypedResults.NoContent();
        }
    }

    internal record AddPlayerScoreRequestParameters(
        [FromRoute] Guid PlayerId,
        AddPlayerScoreRequest Request,
        IMediator Mediator,
        CancellationToken CancellationToken
    );

    public record AddPlayerScoreRequest(
        double Score,
        string Country,
        string FirstName,
        string LastName,
        string LeaderBoardName = Constants.GlobalLeaderBoard
    );
}
