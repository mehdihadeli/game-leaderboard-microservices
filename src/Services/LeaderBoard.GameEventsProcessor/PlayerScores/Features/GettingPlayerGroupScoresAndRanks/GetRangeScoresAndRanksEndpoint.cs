using Humanizer;
using LeaderBoard.GameEventsProcessor.PlayerScores.Dtos;
using LeaderBoard.GameEventsProcessor.Shared;
using LeaderBoard.SharedKernel.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LeaderBoard.GameEventsProcessor.PlayerScores.Features.GettingPlayerGroupScoresAndRanks;

public static class GetPlayerGroupScoresAndRanksEndpoint
{
    internal static RouteHandlerBuilder MapGetPlayerGroupScoresAndRanks(
        this IEndpointRouteBuilder routeBuilder
    )
    {
        return routeBuilder
            .MapGet("players", Handle)
            .WithTags(nameof(PlayerScores))
            .WithName("GetPlayerGroupScoresAndRanks");

        static async Task<Results<Ok<IList<PlayerScoreDto>>, ValidationProblem>> Handle(
            [AsParameters] GetPlayerGroupScoresAndRanksRequestParameter requestParameters
        )
        {
            var (mediator, cancellationToken, playerIds, leaderboardName) = requestParameters;
            var res = await mediator.Send(
                new GetPlayerGroupScoresAndRanks(playerIds, leaderboardName),
                cancellationToken
            );

            return TypedResults.Ok(res);
        }
    }

    internal record GetPlayerGroupScoresAndRanksRequestParameter(
        IMediator Mediator,
        CancellationToken CancellationToken,
        IEnumerable<string> PlayerIds,
        string LeaderBoardName = Constants.GlobalLeaderBoard
    );
}
