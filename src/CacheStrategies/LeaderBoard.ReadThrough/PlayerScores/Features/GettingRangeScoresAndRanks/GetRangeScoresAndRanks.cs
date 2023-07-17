using LeaderBoard.ReadThrough.PlayerScores.Dtos;
using LeaderBoard.ReadThrough.Services;
using LeaderBoard.SharedKernel.Core.Extensions;
using MediatR;

namespace LeaderBoard.ReadThrough.PlayerScores.Features.GettingRangeScoresAndRanks;

internal record GetRangeScoresAndRanks(
    string LeaderBoardName = Constants.GlobalLeaderBoard,
    int Start = 0,
    int End = 9
) : IRequest<IList<PlayerScoreDto>?>;

internal class GetRangeScoresAndRanksHandler
    : IRequestHandler<GetRangeScoresAndRanks, IList<PlayerScoreDto>?>
{
    private readonly IReadThrough _readThrough;

    public GetRangeScoresAndRanksHandler(IReadThrough readThrough)
    {
        _readThrough = readThrough;
    }

    public async Task<IList<PlayerScoreDto>?> Handle(
        GetRangeScoresAndRanks request,
        CancellationToken cancellationToken
    )
    {
        request.NotBeNull();
        bool isDesc = true;

        return await _readThrough.GetRangeScoresAndRanks(
            request.LeaderBoardName,
            request.Start,
            request.End,
            isDesc,
            cancellationToken
        );
    }
}
