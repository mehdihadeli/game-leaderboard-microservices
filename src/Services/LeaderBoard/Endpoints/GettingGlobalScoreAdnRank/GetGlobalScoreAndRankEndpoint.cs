using Humanizer;
using LeaderBoard.Dtos;
using LeaderBoard.Services;
using LeaderBoard.SharedKernel.Application.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LeaderBoard.Endpoints.GettingGlobalScoreAdnRank;

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
            var (playerScoreService, cancellationToken, playerId, leaderboardName) =
                requestParameters;
            var res = await playerScoreService.GetGlobalScoreAndRank(
                leaderboardName,
                playerId,
                true,
                cancellationToken
            );

            return TypedResults.Ok(res);
        }
    }

    internal record GetRangeScoresAndRanksRequestParameter(
        IPlayerScoreService PlayerScoreService,
        CancellationToken CancellationToken,
        string PlayerId,
        string LeaderBoardName = Constants.GlobalLeaderBoard
    );
}
