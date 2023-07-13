using Humanizer;
using LeaderBoard.WriteThrough.Dtos;
using LeaderBoard.WriteThrough.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace LeaderBoard.WriteThrough.Endpoints.PlayerScore.AddingPlayerScore;

internal static class AddPlayerScoreEndpoint
{
    internal static RouteHandlerBuilder MapAddPlayerScoreEndpoint(
        this IEndpointRouteBuilder routeBuilder
    )
    {
        return routeBuilder
            .MapPost("players/{playerId}", Handle)
            .WithTags(nameof(PlayerScore).Pluralize())
            .WithName("AddPlayerScore");

        static async Task<Results<NoContent, BadRequest>> Handle(
            [AsParameters] AddPlayerScoreRequestParameters requestParameters
        )
        {
            var (playerId, req, writeThrough, ct) = requestParameters;

            var res = await writeThrough.AddPlayerScore(
                new PlayerScoreDto(
                    playerId,
                    req.Score,
                    req.LeaderBoardName,
                    null,
                    req.Country,
                    req.FirstName,
                    req.LastName
                )
            );

            if (res == false)
            {
                TypedResults.ValidationProblem(new Dictionary<string, string[]>());
            }

            return TypedResults.NoContent();
        }
    }

    internal record AddPlayerScoreRequestParameters(
        [FromRoute] string PlayerId,
        AddPlayerScoreRequest Request,
        IWriteThrough WriteThrough,
        CancellationToken CancellationToken
    );
}
