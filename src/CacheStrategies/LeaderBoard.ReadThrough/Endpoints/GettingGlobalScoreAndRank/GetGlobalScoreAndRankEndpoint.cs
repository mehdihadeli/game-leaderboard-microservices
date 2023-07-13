using Humanizer;
using LeaderBoard.ReadThrough.Dtos;
using LeaderBoard.ReadThrough.Models;
using LeaderBoard.ReadThrough.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LeaderBoard.ReadThrough.Endpoints.GettingRangeScoresAndRanks;

public static class GetGlobalScoreAndRankEndpoint
{
    internal static RouteHandlerBuilder MapGetGlobalScoreAndRank(
        this IEndpointRouteBuilder routeBuilder
    )
    {
        return routeBuilder
            .MapGet("players/{playerId}", Handle)
            .WithTags(nameof(PlayerScore).Pluralize())
            .WithName("GetGlobalScoreAndRank");

        static async Task<Results<Ok<PlayerScoreDto>, ValidationProblem>> Handle(
            [AsParameters] GetRangeScoresAndRanksRequestParameter requestParameters
        )
        {
            var (readThrough, cancellationToken, playerId, leaderboardName) = requestParameters;
            var res = await readThrough.GetGlobalScoreAndRank(
                leaderboardName,
                playerId,
                true,
                cancellationToken
            );

            return TypedResults.Ok(res);
        }
    }

    internal record GetRangeScoresAndRanksRequestParameter(
        IReadThrough ReadThrough,
        CancellationToken CancellationToken,
        string PlayerId,
        string LeaderBoardName = Constants.GlobalLeaderBoard
    );
}
