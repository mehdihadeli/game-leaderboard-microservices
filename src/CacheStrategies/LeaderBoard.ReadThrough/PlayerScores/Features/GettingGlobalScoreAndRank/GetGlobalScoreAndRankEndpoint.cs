using Humanizer;
using LeaderBoard.ReadThrough.PlayerScores.Dtos;
using LeaderBoard.SharedKernel.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LeaderBoard.ReadThrough.PlayerScores.Features.GettingGlobalScoreAndRank;

public static class GetGlobalScoreAndRankEndpoint
{
    internal static RouteHandlerBuilder MapGetGlobalScoreAndRank(
        this IEndpointRouteBuilder routeBuilder
    )
    {
        return routeBuilder
            .MapGet("players/{playerId}", Handle)
            .WithTags(nameof(PlayerScoreReadModel).Pluralize())
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
        string PlayerId,
        string LeaderBoardName = Constants.GlobalLeaderBoard
    );
}
