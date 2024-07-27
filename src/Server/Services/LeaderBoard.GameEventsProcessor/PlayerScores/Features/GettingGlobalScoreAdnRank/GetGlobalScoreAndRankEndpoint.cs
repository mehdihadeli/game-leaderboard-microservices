using LeaderBoard.GameEventsProcessor.PlayerScores.Dtos;
using LeaderBoard.GameEventsProcessor.Shared;
using LeaderBoard.SharedKernel.Web.ProblemDetail.HttpResults;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LeaderBoard.GameEventsProcessor.PlayerScores.Features.GettingGlobalScoreAdnRank;

public static class GetGlobalScoreAndRankEndpoint
{
    internal static RouteHandlerBuilder MapGetGlobalScoreAndRankEndpoint(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder
            .MapGet("players/{playerId:guid}", Handle)
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
        Guid PlayerId,
        string LeaderBoardName = Constants.GlobalLeaderBoard,
        bool IsDesc = true
    );
}
