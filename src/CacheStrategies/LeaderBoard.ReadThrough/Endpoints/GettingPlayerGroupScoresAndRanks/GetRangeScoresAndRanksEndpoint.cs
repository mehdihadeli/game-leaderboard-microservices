using Humanizer;
using LeaderBoard.ReadThrough.Dtos;
using LeaderBoard.ReadThrough.Models;
using LeaderBoard.ReadThrough.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LeaderBoard.ReadThrough.Endpoints.GettingRangeScoresAndRanks;

public static class GetPlayerGroupScoresAndRanksEndpoint
{
    internal static RouteHandlerBuilder MapGetPlayerGroupScoresAndRanks(
        this IEndpointRouteBuilder routeBuilder
    )
    {
        return routeBuilder
            .MapGet("player-group", Handle)
            .WithTags(nameof(PlayerScore).Pluralize())
            .WithName("GetPlayerGroupScoresAndRanks");

        static async Task<Results<Ok<List<PlayerScoreDto>>, ValidationProblem>> Handle(
            [AsParameters] GetPlayerGroupScoresAndRanksRequestParameter requestParameters
        )
        {
            var (readThrough, cancellationToken, playerIds, leaderboardName) = requestParameters;
            var res = await readThrough.GetPlayerGroupScoresAndRanks(
                leaderboardName,
                playerIds,
                true,
                cancellationToken
            );

            return TypedResults.Ok(res);
        }
    }

    internal record GetPlayerGroupScoresAndRanksRequestParameter(
        IReadThrough ReadThrough,
        CancellationToken CancellationToken,
        IEnumerable<string> PlayerIds,
        string LeaderBoardName = Constants.GlobalLeaderBoard
    );
}
