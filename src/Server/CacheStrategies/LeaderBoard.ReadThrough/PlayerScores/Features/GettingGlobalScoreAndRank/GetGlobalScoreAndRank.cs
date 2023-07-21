using LeaderBoard.ReadThrough.PlayerScores.Dtos;
using LeaderBoard.ReadThrough.Shared;
using LeaderBoard.ReadThrough.Shared.Services;
using LeaderBoard.SharedKernel.Core.Extensions;
using MediatR;

namespace LeaderBoard.ReadThrough.PlayerScores.Features.GettingGlobalScoreAndRank;

internal record GetGlobalScoreAndRank(
    string PlayerId,
    string LeaderBoardName = Constants.GlobalLeaderBoard,
    bool IsDesc = true
) : IRequest<PlayerScoreWithNeighborsDto?>;

internal class GetGlobalScoreAndRankHandler
    : IRequestHandler<GetGlobalScoreAndRank, PlayerScoreWithNeighborsDto?>
{
    private readonly IReadThrough _readThrough;

    public GetGlobalScoreAndRankHandler(IReadThrough readThrough)
    {
        _readThrough = readThrough;
    }

    public async Task<PlayerScoreWithNeighborsDto?> Handle(
        GetGlobalScoreAndRank request,
        CancellationToken cancellationToken
    )
    {
        request.NotBeNull();

        return await _readThrough.GetGlobalScoreAndRank(
            request.LeaderBoardName,
            request.PlayerId,
            request.IsDesc,
            cancellationToken
        );
    }
}
