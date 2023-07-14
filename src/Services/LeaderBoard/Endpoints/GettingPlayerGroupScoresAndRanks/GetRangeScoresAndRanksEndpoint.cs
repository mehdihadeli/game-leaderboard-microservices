using Humanizer;
using LeaderBoard.Dtos;
using LeaderBoard.Services;
using LeaderBoard.SharedKernel.Application.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LeaderBoard.Endpoints.GettingPlayerGroupScoresAndRanks;

public static class GetPlayerGroupScoresAndRanksEndpoint
{
    internal static RouteHandlerBuilder MapGetPlayerGroupScoresAndRanks(
        this IEndpointRouteBuilder routeBuilder
    )
    {
        return routeBuilder
            .MapGet("players", Handle)
            .WithTags(nameof(PlayerScore).Pluralize())
            .WithName("GetPlayerGroupScoresAndRanks");

        static async Task<Results<Ok<List<PlayerScoreDto>>, ValidationProblem>> Handle(
            [AsParameters] GetPlayerGroupScoresAndRanksRequestParameter requestParameters
        )
        {
            var (playerScoreService, cancellationToken, playerIds, leaderboardName) =
                requestParameters;
            var res = await playerScoreService.GetPlayerGroupScoresAndRanks(
                leaderboardName,
                playerIds,
                true,
                cancellationToken
            );

            return TypedResults.Ok(res);
        }
    }

    internal record GetPlayerGroupScoresAndRanksRequestParameter(
        IPlayerScoreService PlayerScoreService,
        CancellationToken CancellationToken,
        IEnumerable<string> PlayerIds,
        string LeaderBoardName = Constants.GlobalLeaderBoard
    );
}
