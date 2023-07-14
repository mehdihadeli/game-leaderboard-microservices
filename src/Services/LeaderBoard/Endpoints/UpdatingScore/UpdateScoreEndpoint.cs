using Humanizer;
using LeaderBoard.Services;
using LeaderBoard.SharedKernel.Application.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace LeaderBoard.Endpoints.UpdatingScore;

internal static class UpdateScoreEndpoint
{
    internal static RouteHandlerBuilder MapUpdateScoreEndpoint(
        this IEndpointRouteBuilder routeBuilder
    )
    {
        return routeBuilder
            .MapPut("players/{playerId}", Handle)
            .WithTags(nameof(PlayerScore).Pluralize())
            .WithName("UpdateScore");

        static async Task<Results<NoContent, BadRequest>> Handle(
            [AsParameters] UpdateScoreRequestParameters requestParameters
        )
        {
            var (playerId, req, playerScoreService, ct) = requestParameters;

            var res = await playerScoreService.UpdateScore(req.LeaderBoardName, playerId, req.Score, ct);

            if (res == false)
            {
                TypedResults.ValidationProblem(new Dictionary<string, string[]>());
            }

            return TypedResults.NoContent();
        }
    }

    internal record UpdateScoreRequestParameters(
        [FromRoute] string PlayerId,
        UpdateScoreRequest Request,
        IPlayerScoreService PlayerScoreService,
        CancellationToken CancellationToken
    );
}
