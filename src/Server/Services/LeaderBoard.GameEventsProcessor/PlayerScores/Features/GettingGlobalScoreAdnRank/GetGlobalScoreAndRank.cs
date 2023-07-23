using Humanizer;
using LeaderBoard.GameEventsProcessor.PlayerScores.Dtos;
using LeaderBoard.GameEventsProcessor.Shared;
using LeaderBoard.GameEventsProcessor.Shared.Clients.ReadThrough;
using LeaderBoard.GameEventsProcessor.Shared.Services;
using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.SharedKernel.Core.Exceptions;
using LeaderBoard.SharedKernel.Core.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using LeaderBoardReadDbContext = LeaderBoard.SharedKernel.Application.Data.EFContext.LeaderBoardReadDbContext;

namespace LeaderBoard.GameEventsProcessor.PlayerScores.Features.GettingGlobalScoreAdnRank;

public record GetGlobalScoreAndRank(Guid PlayerId, string LeaderBoardName, bool IsDesc = true)
    : IRequest<PlayerScoreWithNeighborsDto?>;

internal class GetGlobalScoreAndRankHandler
    : IRequestHandler<GetGlobalScoreAndRank, PlayerScoreWithNeighborsDto?>
{
    private readonly LeaderBoardReadDbContext _leaderBoardReadDbContext;
    private readonly IReadThroughClient _readThroughClient;
    private readonly IPlayerScoreService _playerScoreService;
    private readonly LeaderBoardOptions _leaderboardOptions;
    private readonly IDatabase _redisDatabase;

    public GetGlobalScoreAndRankHandler(
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

    public async Task<PlayerScoreWithNeighborsDto?> Handle(
        GetGlobalScoreAndRank request,
        CancellationToken cancellationToken
    )
    {
        request.NotBeNull();

        string key = $"{nameof(PlayerScoreReadModel).Underscore()}:{request.PlayerId}";

        if (_leaderboardOptions.UseReadThrough)
        {
            return await _readThroughClient.GetGlobalScoreAndRank(
                request.LeaderBoardName,
                request.PlayerId.ToString(),
                request.IsDesc,
                cancellationToken
            );
        }

        var score = await _redisDatabase.SortedSetScoreAsync(request.LeaderBoardName, key);
        var rank = await _redisDatabase.SortedSetRankAsync(
            request.LeaderBoardName,
            key,
            request.IsDesc ? Order.Descending : Order.Ascending
        );

        if ((score == null || rank == null) && _leaderboardOptions.UseReadCacheAside)
        {
            var playerIdString = request.PlayerId.ToString();

            // https://developers.eventstore.com/server/v22.10/projections.html#streams-projection
            // First read from EF postgres database as backup database and then if not exists read from primary database EventStoreDB (but we have some limitations reading all streams and filtering!)
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
                    request.IsDesc ? Order.Descending : Order.Ascending
                );
                rank = request.IsDesc ? rank + 1 : rank - 1;

                var nextMember = await _playerScoreService.GetNextMember(
                    request.LeaderBoardName,
                    key,
                    request.IsDesc
                );
                var previousMember = await _playerScoreService.GetPreviousMember(
                    request.LeaderBoardName,
                    key,
                    request.IsDesc
                );

                return new PlayerScoreWithNeighborsDto(
                    previousMember,
                    new PlayerScoreDto(
                        request.PlayerId.ToString(),
                        playerScore.Score,
                        request.LeaderBoardName,
                        rank,
                        playerScore.FirstName,
                        playerScore.LastName,
                        playerScore.Country
                    ),
                    nextMember
                );
            }

            throw new NotFoundException("PlayerScore not found");
        }
        else
        {
            PlayerScoreDetailDto? detail = await _playerScoreService.GetPlayerScoreDetail(
                request.LeaderBoardName,
                key,
                request.IsDesc,
                cancellationToken
            );
            var nextMember = await _playerScoreService.GetNextMember(
                request.LeaderBoardName,
                key,
                request.IsDesc
            );
            var previousMember = await _playerScoreService.GetPreviousMember(
                request.LeaderBoardName,
                key,
                request.IsDesc
            );

            rank = request.IsDesc ? rank + 1 : rank - 1;

            return new PlayerScoreWithNeighborsDto(
                previousMember,
                new PlayerScoreDto(
                    request.PlayerId.ToString(),
                    score ?? 0,
                    request.LeaderBoardName,
                    rank ?? 1,
                    detail?.FirstName ?? string.Empty,
                    detail?.LastName ?? string.Empty,
                    detail?.Country ?? string.Empty
                ),
                nextMember
            );
        }
    }
}
