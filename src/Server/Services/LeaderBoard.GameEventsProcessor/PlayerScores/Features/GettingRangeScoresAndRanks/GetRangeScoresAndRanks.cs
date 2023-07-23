using AutoMapper;
using LeaderBoard.GameEventsProcessor.PlayerScores.Dtos;
using LeaderBoard.GameEventsProcessor.Shared;
using LeaderBoard.GameEventsProcessor.Shared.Clients.ReadThrough;
using LeaderBoard.GameEventsProcessor.Shared.Services;
using LeaderBoard.SharedKernel.Application.Data.EFContext;
using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.SharedKernel.Core.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LeaderBoard.GameEventsProcessor.PlayerScores.Features.GettingRangeScoresAndRanks;

public record GetRangeScoresAndRanks(
    string LeaderBoardName = Constants.GlobalLeaderBoard,
    int Start = 0,
    int End = 9,
    bool IsDesc = true
) : IRequest<IList<PlayerScoreDto>?>;

internal class GetRangeScoresAndRanksHandler
    : IRequestHandler<GetRangeScoresAndRanks, IList<PlayerScoreDto>?>
{
    private readonly LeaderBoardReadDbContext _leaderBoardReadDbContext;
    private readonly IReadThroughClient _readThroughClient;
    private readonly IPlayerScoreService _playerScoreService;
    private readonly LeaderBoardOptions _leaderboardOptions;
    private readonly IDatabase _redisDatabase;

    public GetRangeScoresAndRanksHandler(
        IMapper mapper,
        IConnectionMultiplexer redisConnection,
        LeaderBoardReadDbContext leaderBoardReadDbContext,
        IReadThroughClient readThroughClient,
        IPlayerScoreService playerScoreService,
        IOptions<LeaderBoardOptions> leaderboardOptions
    )
    {
        _leaderBoardReadDbContext = leaderBoardReadDbContext;
        _readThroughClient = readThroughClient;
        _playerScoreService = playerScoreService;
        _leaderboardOptions = leaderboardOptions.Value;
        _redisDatabase = redisConnection.GetDatabase();
    }

    public async Task<IList<PlayerScoreDto>?> Handle(
        GetRangeScoresAndRanks request,
        CancellationToken cancellationToken
    )
    {
        request.NotBeNull();

        if (_leaderboardOptions.UseReadThrough)
        {
            return await _readThroughClient.GetRangeScoresAndRanks(
                request.LeaderBoardName,
                request.Start,
                request.End,
                request.IsDesc,
                cancellationToken
            );
        }

        var counter = 1;
        var playerScores = new List<PlayerScoreDto>();
        var results = await _redisDatabase.SortedSetRangeByRankWithScoresAsync(
            request.LeaderBoardName,
            request.Start,
            request.End,
            request.IsDesc ? Order.Descending : Order.Ascending
        );

        var startRank = request.Start + 1;

        if ((results == null || results.Length == 0) && _leaderboardOptions.UseReadCacheAside)
        {
            // https://developers.eventstore.com/server/v22.10/projections.html#streams-projection
            // if data is not available on the cache, First read from EF postgres database as backup database and then if not exists read from primary database EventStoreDB (but we have some limitations reading all streams and filtering!)
            IQueryable<PlayerScoreReadModel> postgresItems = request.IsDesc
                ? _leaderBoardReadDbContext.PlayerScores
                    .AsNoTracking()
                    .Where(x => x.LeaderBoardName == Constants.GlobalLeaderBoard)
                    .OrderByDescending(x => x.Score)
                : _leaderBoardReadDbContext.PlayerScores
                    .AsNoTracking()
                    .Where(x => x.LeaderBoardName == Constants.GlobalLeaderBoard)
                    .OrderBy(x => x.Score);

            await _playerScoreService.PopulateCache(postgresItems);

            List<PlayerScoreReadModel> data = await postgresItems
                .Skip(request.Start)
                .Take(request.End + 1)
                .ToListAsync(cancellationToken: cancellationToken);

            if (data.Count == 0)
            {
                return new List<PlayerScoreDto>();
            }

            return data.Select(
                    (x, i) =>
                        new PlayerScoreDto(
                            x.PlayerId,
                            x.Score,
                            request.LeaderBoardName,
                            Rank: i == 0 ? startRank : startRank += counter,
                            x.FirstName,
                            x.LastName,
                            x.Country
                        )
                )
                .ToList();
        }

        foreach (var sortedsetItem in results!)
        {
            string key = sortedsetItem.Element;
            var playerId = key.Split(":")[1];

            // get detail information about saved sortedset score-player
            var detail = await _playerScoreService.GetPlayerScoreDetail(
                request.LeaderBoardName,
                key,
                request.IsDesc,
                cancellationToken
            );

            var playerScore = new PlayerScoreDto(
                playerId,
                sortedsetItem.Score,
                request.LeaderBoardName,
                startRank,
                detail?.FirstName ?? string.Empty,
                detail?.LastName ?? string.Empty,
                detail?.Country ?? string.Empty
            );
            playerScores.Add(playerScore);

            // next rank for next item
            startRank += counter;
        }

        return playerScores;
    }
}
