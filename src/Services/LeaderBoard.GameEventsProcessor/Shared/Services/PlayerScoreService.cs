using Humanizer;
using LeaderBoard.GameEventsProcessor.PlayerScores.Dtos;
using LeaderBoard.GameEventsProcessor.Shared.Clients.ReadThrough;
using LeaderBoard.SharedKernel.Application.Data.EFContext;
using LeaderBoard.SharedKernel.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using NotFoundException = LeaderBoard.SharedKernel.Core.Exceptions.NotFoundException;

namespace LeaderBoard.GameEventsProcessor.Shared.Services;

//https://codeahoy.com/2017/08/11/caching-strategies-and-how-to-choose-the-right-one/
//https://www.gomomento.com/blog/6-common-caching-design-patterns-to-execute-your-caching-strategy
//https://www.gomomento.com/blog/3-crucial-caching-choices-where-when-and-how

public class PlayerScoreService : IPlayerScoreService
{
    private readonly LeaderBoardReadDbContext _leaderBoardReadDbContext;
    private readonly IReadThroughClient _readThroughClient;
    private readonly LeaderBoardOptions _leaderboardOptions;
    private readonly IDatabase _redisDatabase;

    public PlayerScoreService(
        IConnectionMultiplexer redisConnection,
        LeaderBoardReadDbContext leaderBoardReadDbContext,
        IReadThroughClient readThroughClient,
        IOptions<LeaderBoardOptions> leaderboardOptions
    )
    {
        _leaderBoardReadDbContext = leaderBoardReadDbContext;
        _readThroughClient = readThroughClient;
        _leaderboardOptions = leaderboardOptions.Value;
        _redisDatabase = redisConnection.GetDatabase();
    }

    /// <summary>
    ///  Get details information about saved sortedset player score, because sortedset is limited for saving all informations
    /// </summary>
    public async Task<PlayerScoreDetailDto?> GetPlayerScoreDetail(
        string leaderboardName,
        string playerIdKey,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    )
    {
        if (_leaderboardOptions.UseReadThrough)
        {
            var res = await _readThroughClient.GetGlobalScoreAndRank(
                leaderboardName,
                playerIdKey,
                isDesc,
                cancellationToken
            );
            return res is null
                ? null
                : new PlayerScoreDetailDto(res.Country, res.FirstName, res.LastName);
        }

        HashEntry[] item = await _redisDatabase.HashGetAllAsync(playerIdKey);
        if (item.Length == 0 && _leaderboardOptions.UseReadCacheAside)
        {
            string playerId = playerIdKey.Split(":")[1];

            var playerScore = await _leaderBoardReadDbContext.PlayerScores.SingleOrDefaultAsync(
                x => x.PlayerId == playerId && x.LeaderBoardName == leaderboardName,
                cancellationToken: cancellationToken
            );
            if (playerScore != null)
            {
                await PopulateCache(playerScore);
                return new PlayerScoreDetailDto(
                    playerScore.Country,
                    playerScore.FirstName,
                    playerScore.LastName
                );
            }

            return null;
        }

        var firstName = item.SingleOrDefault(
            x => x.Name == nameof(PlayerScoreReadModel.FirstName).Underscore()
        );
        var lastName = item.SingleOrDefault(
            x => x.Name == nameof(PlayerScoreReadModel.LastName).Underscore()
        );
        var country = item.SingleOrDefault(
            x => x.Name == nameof(PlayerScoreReadModel.Country).Underscore()
        );

        return new PlayerScoreDetailDto(country.Value, firstName.Value, lastName.Value);
    }

    public async Task PopulateCache(PlayerScoreReadModel playerScore)
    {
        var key = $"{nameof(PlayerScoreReadModel).Underscore()}:{playerScore.PlayerId}";

        // store the summary of our player-score in sortedset, it cannot store all information
        await _redisDatabase.SortedSetAddAsync(playerScore.LeaderBoardName, key, playerScore.Score);

        // store detail of out score-player in hashset. it is related to its score information with their same unique identifier
        await _redisDatabase.HashSetAsync(
            key,
            new HashEntry[]
            {
                new(nameof(PlayerScoreReadModel.Country).Underscore(), playerScore.Country),
                new(nameof(PlayerScoreReadModel.FirstName).Underscore(), playerScore.FirstName),
                new(nameof(PlayerScoreReadModel.LastName).Underscore(), playerScore.LastName),
            }
        );
    }

    public async Task PopulateCache(IQueryable<PlayerScoreReadModel> databaseQuery)
    {
        await foreach (var playerScore in LoadDatabaseItemByPaging(databaseQuery))
        {
            await PopulateCache(playerScore);
        }
    }

    //https://dev.to/mbernard/asynchronous-streams-in-c-8-0-33la
    private async IAsyncEnumerable<PlayerScoreReadModel> LoadDatabaseItemByPaging(
        IQueryable<PlayerScoreReadModel> databaseQuery
    )
    {
        int pageNumber = 0;
        int pageSize = 1000;
        bool hasMoreData = true;

        while (hasMoreData)
        {
            var data = await databaseQuery.Skip(pageNumber * pageSize).Take(pageSize).ToListAsync();

            foreach (var entity in data)
            {
                yield return entity;
            }

            hasMoreData = data.Count == pageSize;
            pageNumber++;
        }
    }

    public RedisValue GetNextMember(string leaderBoardName, string memberKey, bool isDesc = true)
    {
        var rank = _redisDatabase.SortedSetRank(leaderBoardName, memberKey);
        if (rank is null)
            throw new NotFoundException($"Could not find rank for {memberKey} member");

        var nextElement = _redisDatabase
            .SortedSetRangeByRank(
                leaderBoardName,
                start: (long)rank,
                (long)rank + 1,
                isDesc ? Order.Descending : Order.Ascending
            )
            .Skip(1);

        var val = nextElement.FirstOrDefault();
        return val;
    }

    public RedisValue GetPreviousMember(
        string leaderBoardName,
        string memberKey,
        bool isDesc = true
    )
    {
        var rank = _redisDatabase.SortedSetRank(leaderBoardName, memberKey);
        if (rank is null)
            throw new NotFoundException($"Could not find rank for {memberKey} member");

        var nextElement = _redisDatabase.SortedSetRangeByRank(
            leaderBoardName,
            start: (long)rank - 1,
            (long)rank,
            isDesc ? Order.Descending : Order.Ascending
        );

        var val = nextElement.FirstOrDefault();

        return val;
    }
}
