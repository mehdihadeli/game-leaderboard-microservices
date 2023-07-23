using LeaderBoard.SharedKernel.Core.Extensions;
using LeaderBoard.WriteThrough.PlayerScore.Dtos;
using LeaderBoard.WriteThrough.Shared.Services;
using MediatR;

namespace LeaderBoard.WriteThrough.PlayerScore.Features.AddingOrUpdatingPlayerScore;

public record AddOrUpdatePlayerScore(
    string PlayerId,
    double Score,
    string LeaderBoardName,
    string? Country,
    string? FirstName,
    string? LastName
) : IRequest;

internal class AddOrUpdatePlayerScoreHandler : IRequestHandler<AddOrUpdatePlayerScore>
{
    private readonly IWriteThrough _writeThrough;

    public AddOrUpdatePlayerScoreHandler(IWriteThrough writeThrough)
    {
        _writeThrough = writeThrough;
    }

    public async Task Handle(AddOrUpdatePlayerScore request, CancellationToken cancellationToken)
    {
        request.NotBeNull();

        await _writeThrough.AddOrUpdatePlayerScore(
            new PlayerScoreDto(
                request.PlayerId,
                request.Score,
                request.LeaderBoardName,
                request.FirstName ?? String.Empty,
                request.LastName ?? String.Empty,
                request.Country ?? String.Empty
            ),
            cancellationToken
        );
    }
}
