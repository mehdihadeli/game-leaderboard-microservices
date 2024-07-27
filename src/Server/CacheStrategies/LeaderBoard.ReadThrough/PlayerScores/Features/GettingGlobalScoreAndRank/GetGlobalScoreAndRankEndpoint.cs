using LeaderBoard.ReadThrough.PlayerScores.Dtos;
using LeaderBoard.ReadThrough.Shared;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LeaderBoard.ReadThrough.PlayerScores.Features.GettingGlobalScoreAndRank;

public static class GetGlobalScoreAndRankEndpoint
{
    internal static RouteHandlerBuilder MapGetGlobalScoreAndRank(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder
            .MapGet("players/{playerId}", Handle)
            .WithTags(nameof(PlayerScores))
            .WithName(nameof(GetGlobalScoreAndRank));

        static async Task<Results<Ok<PlayerScoreWithNeighborsDto>, ValidationProblem, ProblemHttpResult>> Handle(
            [AsParameters] GetRangeScoresAndRanksRequestParameter requestParameters
        )
        {
            var (mediator, cancellationToken, playerId, leaderboardName, isDesc) = requestParameters;
            var res = await mediator.Send(
                new GetGlobalScoreAndRank(playerId, leaderboardName, isDesc),
                cancellationToken
            );

            return TypedResults.Ok(res);
        }
    }

    internal record GetRangeScoresAndRanksRequestParameter(
        IMediator Mediator,
        CancellationToken CancellationToken,
        string PlayerId,
        string LeaderBoardName = Constants.GlobalLeaderBoard,
        bool IsDesc = true
    );
}
