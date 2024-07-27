using LeaderBoard.ReadThrough.PlayerScores.Dtos;
using LeaderBoard.ReadThrough.Shared;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LeaderBoard.ReadThrough.PlayerScores.Features.GettingPlayerGroupGlobalScoresAndRanks;

public static class GetPlayerGroupGlobalScoresAndRanksEndpoint
{
    internal static RouteHandlerBuilder MapGetPlayerGroupGlobalScoresAndRanksEndpoints(
        this IEndpointRouteBuilder routeBuilder
    )
    {
        return routeBuilder
            .MapGet("players", Handle)
            .WithTags(nameof(PlayerScores))
            .WithName(nameof(GetPlayerGroupGlobalScoresAndRanks));

        static async Task<Results<Ok<IList<PlayerScoreWithNeighborsDto>>, ValidationProblem>> Handle(
            [AsParameters] GetPlayerGroupGlobalScoresAndRanksRequestParameter requestParameters
        )
        {
            var (mediator, cancellationToken, playerIds, leaderboardName, isDesc) = requestParameters;
            var res = await mediator.Send(
                new GetPlayerGroupGlobalScoresAndRanks(playerIds, leaderboardName, isDesc),
                cancellationToken
            );

            return TypedResults.Ok(res);
        }
    }

    internal record GetPlayerGroupGlobalScoresAndRanksRequestParameter(
        IMediator Mediator,
        CancellationToken CancellationToken,
        string[] PlayerIds,
        string LeaderBoardName = Constants.GlobalLeaderBoard,
        bool IsDesc = true
    );
}
