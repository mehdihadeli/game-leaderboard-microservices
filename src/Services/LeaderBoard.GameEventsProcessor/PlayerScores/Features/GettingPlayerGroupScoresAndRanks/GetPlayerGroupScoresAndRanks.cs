using LeaderBoard.GameEventsProcessor.PlayerScores.Dtos;
using LeaderBoard.GameEventsProcessor.PlayerScores.Features.GettingGlobalScoreAdnRank;
using LeaderBoard.GameEventsProcessor.Shared;
using LeaderBoard.GameEventsProcessor.Shared.Clients.ReadThrough;
using LeaderBoard.GameEventsProcessor.Shared.Services;
using LeaderBoard.SharedKernel.Application.Data.EFContext;
using MediatR;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LeaderBoard.GameEventsProcessor.PlayerScores.Features.GettingPlayerGroupScoresAndRanks;

internal record GetPlayerGroupScoresAndRanks(
    IEnumerable<string> PlayerIds,
    string LeaderBoardName = Constants.GlobalLeaderBoard
) : IRequest<IList<PlayerScoreDto>?>;

internal class GetPlayerGroupScoresAndRanksHandler
    : IRequestHandler<GetPlayerGroupScoresAndRanks, IList<PlayerScoreDto>?>
{
    private readonly LeaderBoardReadDbContext _leaderBoardReadDbContext;
    private readonly IReadThroughClient _readThroughClient;
    private readonly IPlayerScoreService _playerScoreService;
    private readonly IMediator _mediator;
    private readonly LeaderBoardOptions _leaderboardOptions;
    private readonly IDatabase _redisDatabase;

    public GetPlayerGroupScoresAndRanksHandler(
        IConnectionMultiplexer redisConnection,
        LeaderBoardReadDbContext leaderBoardReadDbContext,
        IReadThroughClient readThroughClient,
        IPlayerScoreService playerScoreService,
        IMediator mediator,
        IOptions<LeaderBoardOptions> leaderboardOptions
    )
    {
        _leaderBoardReadDbContext = leaderBoardReadDbContext;
        _readThroughClient = readThroughClient;
        _playerScoreService = playerScoreService;
        _mediator = mediator;
        _leaderboardOptions = leaderboardOptions.Value;
        _redisDatabase = redisConnection.GetDatabase();
    }

    public async Task<IList<PlayerScoreDto>?> Handle(
        GetPlayerGroupScoresAndRanks request,
        CancellationToken cancellationToken
    )
    {
        bool isDesc = true;
        if (_leaderboardOptions.UseReadThrough)
        {
            return await _readThroughClient.GetPlayerGroupScoresAndRanks(
                request.LeaderBoardName,
                request.PlayerIds,
                isDesc,
                cancellationToken
            );
        }

        var results = new List<PlayerScoreDto>();
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

        List<PlayerScoreDto> items = isDesc
            ? results.OrderByDescending(x => x.Score).ToList()
            : results.OrderBy(x => x.Score).ToList();
        var counter = isDesc ? 1 : -1;
        var startRank = isDesc ? 1 : items.Count;

        return items
            .Select((x, i) => x with { Rank = i == 0 ? startRank : counter + startRank })
            .ToList();
    }
}
