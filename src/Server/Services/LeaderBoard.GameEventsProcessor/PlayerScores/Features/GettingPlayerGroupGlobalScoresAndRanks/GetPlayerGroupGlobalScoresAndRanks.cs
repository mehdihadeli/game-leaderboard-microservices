using LeaderBoard.GameEventsProcessor.PlayerScores.Dtos;
using LeaderBoard.GameEventsProcessor.PlayerScores.Features.GettingGlobalScoreAdnRank;
using LeaderBoard.GameEventsProcessor.Shared;
using LeaderBoard.GameEventsProcessor.Shared.Clients.ReadThrough;
using MediatR;
using Microsoft.Extensions.Options;

namespace LeaderBoard.GameEventsProcessor.PlayerScores.Features.GettingPlayerGroupGlobalScoresAndRanks;

internal record GetPlayerGroupGlobalScoresAndRanks(
    IEnumerable<string> PlayerIds,
    string LeaderBoardName = Constants.GlobalLeaderBoard,
    bool IsDesc = true
) : IRequest<IList<PlayerScoreWithNeighborsDto>?>;

internal class GetPlayerGroupGlobalScoresAndRanksHandler
    : IRequestHandler<GetPlayerGroupGlobalScoresAndRanks, IList<PlayerScoreWithNeighborsDto>?>
{
    private readonly IReadThroughClient _readThroughClient;
    private readonly IMediator _mediator;
    private readonly LeaderBoardOptions _leaderboardOptions;

    public GetPlayerGroupGlobalScoresAndRanksHandler(
        IReadThroughClient readThroughClient,
        IMediator mediator,
        IOptions<LeaderBoardOptions> leaderboardOptions
    )
    {
        _readThroughClient = readThroughClient;
        _mediator = mediator;
        _leaderboardOptions = leaderboardOptions.Value;
    }

    public async Task<IList<PlayerScoreWithNeighborsDto>?> Handle(
        GetPlayerGroupGlobalScoresAndRanks request,
        CancellationToken cancellationToken
    )
    {
        if (_leaderboardOptions.UseReadThrough)
        {
            return await _readThroughClient.GetPlayerGroupGlobalScoresAndRanks(
                request.LeaderBoardName,
                request.PlayerIds,
                request.IsDesc,
                cancellationToken
            );
        }

        var results = new List<PlayerScoreWithNeighborsDto>();
        foreach (var playerId in request.PlayerIds)
        {
            var playerScore = await _mediator.Send(
                new GetGlobalScoreAndRank(Guid.Parse(playerId), request.LeaderBoardName),
                cancellationToken
            );
            if (playerScore != null)
            {
                results.Add(playerScore);
            }
        }

        List<PlayerScoreWithNeighborsDto> items = request.IsDesc
            ? results.OrderByDescending(x => x.CurrentPlayerScore.Score).ToList()
            : results.OrderBy(x => x.CurrentPlayerScore.Score).ToList();

        return items;
    }
}
