using Humanizer;
using LeaderBoard.ReadThrough.PlayerScores.Dtos;
using LeaderBoard.SharedKernel.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LeaderBoard.ReadThrough.PlayerScores.Features.GettingRangeScoresAndRanks;

public static class GetRangeScoresAndRanksEndpoint
{
    internal static RouteHandlerBuilder MapGetRangeScoresAndRanks(
        this IEndpointRouteBuilder routeBuilder
    )
    {
        return routeBuilder
            .MapGet("range", Handle)
            .WithTags(nameof(PlayerScoreReadModel).Pluralize())
            .WithName("GetRangeScoresAndRanks");

        static async Task<Results<Ok<IList<PlayerScoreDto>>, ValidationProblem>> Handle(
            [AsParameters] GetRangeScoresAndRanksRequestParameter requestParameters
        )
        {
            var (mediator, cancellationToken, leaderboardName, start, end) = requestParameters;
            var res = await mediator.Send(
                new GetRangeScoresAndRanks(leaderboardName, start, end),
                cancellationToken
            );

            return TypedResults.Ok(res);
        }
    }

    internal record GetRangeScoresAndRanksRequestParameter(
        IMediator Mediator,
        CancellationToken CancellationToken,
        string LeaderBoardName = Constants.GlobalLeaderBoard,
        int Start = 0,
        int End = 9
    );
}
