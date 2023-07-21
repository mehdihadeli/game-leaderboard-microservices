using LeaderBoard.ReadThrough.PlayerScores.Dtos;
using LeaderBoard.ReadThrough.Shared;
using LeaderBoard.ReadThrough.Shared.Services;
using LeaderBoard.SharedKernel.Core.Extensions;
using MediatR;

namespace LeaderBoard.ReadThrough.PlayerScores.Features.GettingPlayerGroupGlobalScoresAndRanks;

internal record GetPlayerGroupGlobalScoresAndRanks(
    IEnumerable<string> PlayerIds,
    string LeaderBoardName = Constants.GlobalLeaderBoard,
    bool IsDesc = true
) : IRequest<IList<PlayerScoreWithNeighborsDto>?>;

internal class GetRangeScoresAndRanksHandler
    : IRequestHandler<GetPlayerGroupGlobalScoresAndRanks, IList<PlayerScoreWithNeighborsDto>?>
{
    private readonly IReadThrough _readThrough;

    public GetRangeScoresAndRanksHandler(IReadThrough readThrough)
    {
        _readThrough = readThrough;
    }

    public async Task<IList<PlayerScoreWithNeighborsDto>?> Handle(
        GetPlayerGroupGlobalScoresAndRanks request,
        CancellationToken cancellationToken
    )
    {
        request.NotBeNull();

        return await _readThrough.GetPlayerGroupGlobalScoresAndRanks(
            request.LeaderBoardName,
            request.PlayerIds,
            request.IsDesc,
            cancellationToken
        );
    }
}
