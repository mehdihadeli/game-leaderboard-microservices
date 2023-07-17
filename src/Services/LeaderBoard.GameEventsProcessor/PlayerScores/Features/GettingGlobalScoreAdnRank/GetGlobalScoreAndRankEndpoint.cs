using LeaderBoard.GameEventsProcessor.PlayerScores.Dtos;
using LeaderBoard.GameEventsProcessor.Shared;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LeaderBoard.GameEventsProcessor.PlayerScores.Features.GettingGlobalScoreAdnRank;

public static class GetGlobalScoreAndRankEndpoint
{
    internal static RouteHandlerBuilder MapGetGlobalScoreAndRank(
        this IEndpointRouteBuilder routeBuilder
    )
    {
        return routeBuilder
            .MapGet("players/{playerId:guid}", Handle)
            .WithTags(nameof(PlayerScores))
            .WithName("GetGlobalScoreAndRank");

        static async Task<Results<Ok<PlayerScoreDto>, ValidationProblem>> Handle(
            [AsParameters] GetRangeScoresAndRanksRequestParameter requestParameters
        )
        {
            var (mediator, cancellationToken, playerId, leaderboardName) = requestParameters;

            var res = await mediator.Send(
                new GetGlobalScoreAndRank(playerId, leaderboardName),
                cancellationToken
            );

            return TypedResults.Ok(res);
        }
    }

    internal record GetRangeScoresAndRanksRequestParameter(
        IMediator Mediator,
        CancellationToken CancellationToken,
        Guid PlayerId,
        string LeaderBoardName = Constants.GlobalLeaderBoard
    );
}
