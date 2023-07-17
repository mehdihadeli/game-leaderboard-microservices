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
    int End = 9
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

        var isDesc = true;

        if (_leaderboardOptions.UseReadThrough)
        {
            return await _readThroughClient.GetRangeScoresAndRanks(
                request.LeaderBoardName,
                request.Start,
                request.End,
                isDesc,
                cancellationToken
            );
        }

        var counter = isDesc ? 1 : -1;
        var playerScores = new List<PlayerScoreDto>();
        var results = await _redisDatabase.SortedSetRangeByRankWithScoresAsync(
            request.LeaderBoardName,
            request.Start,
            request.End,
            isDesc ? Order.Descending : Order.Ascending
        );

        var startRank = isDesc ? request.Start + 1 : results.Length;

        if ((results == null || results.Length == 0) && _leaderboardOptions.UseReadCacheAside)
        {
            // if data is not available on the cache, at first we try to get items from EF postgres read database and if not available we should go search on EventStoreDB
            IQueryable<PlayerScoreReadModel> postgresItems = isDesc
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
                //// Search on EventStoreDB
                // data = fetchedDataFromEsdb
                // if (data.Count == 0)
                // {
                //     return null;
                // }

                return null;
            }

            startRank = isDesc ? request.Start + 1 : data.Count;
            return data.Select(
                    (x, i) =>
                        new PlayerScoreDto(
                            x.PlayerId,
                            x.Score,
                            request.LeaderBoardName,
                            Rank: i == 0 ? startRank : startRank += counter,
                            x.Country,
                            x.FirstName,
                            x.LeaderBoardName
                        )
                )
                .ToList();
        }

        if (results is null)
            return null;

        foreach (var sortedsetItem in results)
        {
            string key = sortedsetItem.Element;
            var playerId = key.Split(":")[1];

            // get detail information about saved sortedset score-player
            var detail = await _playerScoreService.GetPlayerScoreDetail(
                request.LeaderBoardName,
                key,
                isDesc,
                cancellationToken
            );

            var playerScore = new PlayerScoreDto(
                playerId,
                sortedsetItem.Score,
                request.LeaderBoardName,
                startRank,
                detail?.Country,
                detail?.FirstName,
                detail?.LastName
            );
            playerScores.Add(playerScore);

            // next rank for next item
            startRank += counter;
        }

        return playerScores;
    }
}
