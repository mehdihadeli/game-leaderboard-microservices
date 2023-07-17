using Humanizer;
using LeaderBoard.ReadThrough.PlayerScores.Dtos;
using LeaderBoard.SharedKernel.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LeaderBoard.ReadThrough.PlayerScores.Features.GettingPlayerGroupScoresAndRanks;

public static class GetPlayerGroupScoresAndRanksEndpoint
{
    internal static RouteHandlerBuilder MapGetPlayerGroupScoresAndRanks(
        this IEndpointRouteBuilder routeBuilder
    )
    {
        return routeBuilder
            .MapGet("players", Handle)
            .WithTags(nameof(PlayerScoreReadModel).Pluralize())
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
