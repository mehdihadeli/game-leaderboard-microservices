using Humanizer;
using LeaderBoard.GameEventsProcessor.PlayerScores.Dtos;
using LeaderBoard.GameEventsProcessor.Shared;
using LeaderBoard.GameEventsProcessor.Shared.Clients.ReadThrough;
using LeaderBoard.GameEventsProcessor.Shared.Services;
using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.SharedKernel.Contracts.Data.EventStore;
using LeaderBoard.SharedKernel.Core.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using LeaderBoardReadDbContext = LeaderBoard.SharedKernel.Application.Data.EFContext.LeaderBoardReadDbContext;

namespace LeaderBoard.GameEventsProcessor.PlayerScores.Features.GettingGlobalScoreAdnRank;

public record GetGlobalScoreAndRank(Guid PlayerId, string LeaderBoardName)
    : IRequest<PlayerScoreDto?>;

internal class GetGlobalScoreAndRankHandler
    : IRequestHandler<GetGlobalScoreAndRank, PlayerScoreDto?>
{
    private readonly LeaderBoardReadDbContext _leaderBoardReadDbContext;
    private readonly IReadThroughClient _readThroughClient;
    private readonly IAggregateStore _aggregateStore;
    private readonly IPlayerScoreService _playerScoreService;
    private readonly LeaderBoardOptions _leaderboardOptions;
    private readonly IDatabase _redisDatabase;

    public GetGlobalScoreAndRankHandler(
        IConnectionMultiplexer redisConnection,
        LeaderBoardReadDbContext leaderBoardReadDbContext,
        IReadThroughClient readThroughClient,
        IAggregateStore aggregateStore,
        IPlayerScoreService playerScoreService,
        IOptions<LeaderBoardOptions> leaderboardOptions
    )
    {
        _leaderBoardReadDbContext = leaderBoardReadDbContext;
        _readThroughClient = readThroughClient;
        _aggregateStore = aggregateStore;
        _playerScoreService = playerScoreService;
        _leaderboardOptions = leaderboardOptions.Value;
        _redisDatabase = redisConnection.GetDatabase();
    }

    public async Task<PlayerScoreDto?> Handle(
        GetGlobalScoreAndRank request,
        CancellationToken cancellationToken
    )
    {
        request.NotBeNull();

        bool isDesc = true;
        string key = $"{nameof(PlayerScoreReadModel).Underscore()}:{request.PlayerId}";

        if (_leaderboardOptions.UseReadThrough)
        {
            return await _readThroughClient.GetGlobalScoreAndRank(
                request.LeaderBoardName,
                key,
                isDesc,
                cancellationToken
            );
        }

        var score = await _redisDatabase.SortedSetScoreAsync(request.LeaderBoardName, key);
        var rank = await _redisDatabase.SortedSetRankAsync(
            request.LeaderBoardName,
            key,
            isDesc ? Order.Descending : Order.Ascending
        );
        rank = isDesc ? rank + 1 : rank - 1;

        if ((score == null || rank == null) && _leaderboardOptions.UseReadCacheAside)
        {
            var playerIdString = request.PlayerId.ToString();

            // First read from EF postgres database as backup database and then if not exists read from primary database EventStoreDB
            var playerScore = await _leaderBoardReadDbContext.PlayerScores.SingleOrDefaultAsync(
                x =>
                    x.PlayerId == playerIdString
                    && x.LeaderBoardName == Constants.GlobalLeaderBoard,
                cancellationToken: cancellationToken
            );
            if (playerScore != null)
            {
                await _playerScoreService.PopulateCache(playerScore);

                rank = await _redisDatabase.SortedSetRankAsync(
                    request.LeaderBoardName,
                    key,
                    isDesc ? Order.Descending : Order.Ascending
                );
                rank = isDesc ? rank + 1 : rank - 1;

                return new PlayerScoreDto(
                    request.PlayerId.ToString(),
                    playerScore.Score,
                    request.LeaderBoardName,
                    rank,
                    playerScore.Country,
                    playerScore.FirstName,
                    playerScore.LastName
                );
            }

            if (playerScore == null)
            {
                // data not exists on EF Postgres so we search on EventStoreDB
                var playerScoreAggregate = await _aggregateStore.GetAsync<
                    PlayerScoreAggregate,
                    string
                >(playerIdString, cancellationToken);
                if (playerScoreAggregate != null)
                {
                    await _playerScoreService.PopulateCache(
                        new PlayerScoreReadModel
                        {
                            PlayerId = playerScoreAggregate.Id,
                            Score = playerScoreAggregate.Score,
                            LeaderBoardName = playerScoreAggregate.LeaderBoardName,
                            FirstName = playerScoreAggregate.FirstName,
                            LastName = playerScoreAggregate.LastName,
                            Country = playerScoreAggregate.Country,
                            CreatedAt = playerScoreAggregate.Created,
                        }
                    );

                    rank = await _redisDatabase.SortedSetRankAsync(
                        request.LeaderBoardName,
                        key,
                        isDesc ? Order.Descending : Order.Ascending
                    );
                    rank = isDesc ? rank + 1 : rank - 1;

                    return new PlayerScoreDto(
                        playerScoreAggregate.Id,
                        playerScoreAggregate.Score,
                        playerScoreAggregate.LeaderBoardName,
                        rank,
                        playerScoreAggregate.Country,
                        playerScoreAggregate.FirstName,
                        playerScoreAggregate.LastName
                    );
                }
            }

            return null;
        }

        PlayerScoreDetailDto? detail = await _playerScoreService.GetPlayerScoreDetail(
            request.LeaderBoardName,
            key,
            isDesc,
            cancellationToken
        );
        return new PlayerScoreDto(
            request.PlayerId.ToString(),
            score ?? 0,
            request.LeaderBoardName,
            rank ?? 1,
            detail?.Country,
            detail?.FirstName,
            detail?.LastName
        );
    }
}
