using LeaderBoard.ReadThrough.PlayerScores.Dtos;
using LeaderBoard.ReadThrough.Services;
using LeaderBoard.SharedKernel.Core.Extensions;
using MediatR;

namespace LeaderBoard.ReadThrough.PlayerScores.Features.GettingPlayerGroupScoresAndRanks;

internal record GetPlayerGroupScoresAndRanks(
    IEnumerable<string> PlayerIds,
    string LeaderBoardName = Constants.GlobalLeaderBoard
) : IRequest<IList<PlayerScoreDto>?>;

internal class GetRangeScoresAndRanksHandler
    : IRequestHandler<GetPlayerGroupScoresAndRanks, IList<PlayerScoreDto>?>
{
    private readonly IReadThrough _readThrough;

    public GetRangeScoresAndRanksHandler(IReadThrough readThrough)
    {
        _readThrough = readThrough;
    }

    public async Task<IList<PlayerScoreDto>?> Handle(
        GetPlayerGroupScoresAndRanks request,
        CancellationToken cancellationToken
    )
    {
        request.NotBeNull();
        bool isDesc = true;

        return await _readThrough.GetPlayerGroupScoresAndRanks(
            request.LeaderBoardName,
            request.PlayerIds,
            isDesc,
            cancellationToken
        );
    }
}
