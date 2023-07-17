using LeaderBoard.ReadThrough.PlayerScores.Dtos;
using LeaderBoard.ReadThrough.Services;
using LeaderBoard.SharedKernel.Core.Extensions;
using MediatR;

namespace LeaderBoard.ReadThrough.PlayerScores.Features.GettingGlobalScoreAndRank;

internal record GetGlobalScoreAndRank(
    string PlayerId,
    string LeaderBoardName = Constants.GlobalLeaderBoard
) : IRequest<PlayerScoreDto?>;

internal class GetGlobalScoreAndRankHandler
    : IRequestHandler<GetGlobalScoreAndRank, PlayerScoreDto?>
{
    private readonly IReadThrough _readThrough;

    public GetGlobalScoreAndRankHandler(IReadThrough readThrough)
    {
        _readThrough = readThrough;
    }

    public async Task<PlayerScoreDto?> Handle(
        GetGlobalScoreAndRank request,
        CancellationToken cancellationToken
    )
    {
        request.NotBeNull();
        bool isDesc = true;

        return await _readThrough.GetGlobalScoreAndRank(
            request.LeaderBoardName,
            request.PlayerId,
            isDesc,
            cancellationToken
        );
    }
}
