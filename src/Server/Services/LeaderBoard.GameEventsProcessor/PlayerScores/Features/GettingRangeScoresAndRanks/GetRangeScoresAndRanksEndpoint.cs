using LeaderBoard.GameEventsProcessor.PlayerScores.Dtos;
using LeaderBoard.GameEventsProcessor.Shared;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LeaderBoard.GameEventsProcessor.PlayerScores.Features.GettingRangeScoresAndRanks;

public static class GetRangeScoresAndRanksEndpoint
{
    internal static RouteHandlerBuilder MapGetRangeScoresAndRanksEndpoint(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder
            .MapGet("range", Handle)
            .WithTags(nameof(PlayerScores))
            .WithName(nameof(GetRangeScoresAndRanks));

        static async Task<Results<Ok<IList<PlayerScoreDto>>, ValidationProblem>> Handle(
            [AsParameters] GetRangeScoresAndRanksRequestParameter requestParameters
        )
        {
            var (mediator, cancellationToken, leaderboardName, start, end, isDesc) = requestParameters;
            var res = await mediator.Send(
                new GetRangeScoresAndRanks(leaderboardName, start, end, isDesc),
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
        int End = 9,
        bool IsDesc = true
    );
}
