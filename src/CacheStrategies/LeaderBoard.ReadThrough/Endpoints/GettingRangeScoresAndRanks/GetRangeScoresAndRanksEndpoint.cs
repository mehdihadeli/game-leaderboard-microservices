using Humanizer;
using LeaderBoard.ReadThrough.Dtos;
using LeaderBoard.ReadThrough.Services;
using LeaderBoard.SharedKernel.Application.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LeaderBoard.ReadThrough.Endpoints.GettingRangeScoresAndRanks;

public static class GetRangeScoresAndRanksEndpoint
{
    internal static RouteHandlerBuilder MapGetRangeScoresAndRanks(
        this IEndpointRouteBuilder routeBuilder
    )
    {
        return routeBuilder
            .MapGet("range", Handle)
            .WithTags(nameof(PlayerScore).Pluralize())
            .WithName("GetRangeScoresAndRanks");

        static async Task<Results<Ok<List<PlayerScoreDto>>, ValidationProblem>> Handle(
            [AsParameters] GetRangeScoresAndRanksRequestParameter requestParameters
        )
        {
            var (readThrough, cancellationToken, leaderboardName, start, end) = requestParameters;
            var res = await readThrough.GetRangeScoresAndRanks(
                leaderboardName,
                start,
                end,
                true,
                cancellationToken
            );

            return TypedResults.Ok(res);
        }
    }

    internal record GetRangeScoresAndRanksRequestParameter(
        IReadThrough ReadThrough,
        CancellationToken CancellationToken,
        string LeaderBoardName = Constants.GlobalLeaderBoard,
        int Start = 0,
        int End = 9
    );
}
