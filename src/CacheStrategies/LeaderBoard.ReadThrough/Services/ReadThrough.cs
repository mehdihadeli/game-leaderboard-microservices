using System.Globalization;
using Humanizer;
using LeaderBoard.ReadThrough.Dtos;
using LeaderBoard.ReadThrough.Models;
using LeaderBoard.ReadThrough.Providers;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace LeaderBoard.ReadThrough.Services;

//https://codeahoy.com/2017/08/11/caching-strategies-and-how-to-choose-the-right-one/
//https://www.gomomento.com/blog/6-common-caching-design-patterns-to-execute-your-caching-strategy
//https://www.gomomento.com/blog/3-crucial-caching-choices-where-when-and-how
public class ReadThrough : IReadThrough
{
    private readonly IReadProviderDatabase _readProviderDatabase;
    private readonly IDatabase _redisDatabase;

    public ReadThrough(
        IConnectionMultiplexer redisConnection,
        IReadProviderDatabase readProviderDatabase
    )
    {
        _readProviderDatabase = readProviderDatabase;
        _redisDatabase = redisConnection.GetDatabase();
    }

    public async Task<List<PlayerScoreDto>?> GetRangeScoresAndRanks(
        string leaderBoardName,
        int start,
        int end,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    )
    {
        var counter = isDesc ? 1 : -1;
        var playerScores = new List<PlayerScoreDto>();

        // 1. Read data form the cache
        var results = await _redisDatabase.SortedSetRangeByRankWithScoresAsync(
            leaderBoardName,
            start,
            end,
            isDesc ? Order.Descending : Order.Ascending
        );

        if (results == null || results.Length == 0)
        {
            // 2. If data not exist in the cache, read data from primary database
            var items = _readProviderDatabase.GetScoresAndRanks(
                leaderBoardName,
                isDesc,
                cancellationToken
            );

            // 3. update cache with fetched results from primary database
            await PopulateCache(items);
            var data = await items
                .Skip(start)
                .Take(end + 1)
                .ToListAsync(cancellationToken: cancellationToken);

            if (data.Count == 0)
            {
                return null;
            }

            var startRank = isDesc ? start + 1 : data.Count;
            return data.Select(
                    (x, i) =>
                        new PlayerScoreDto(
                            x.PlayerId,
                            x.Score,
                            leaderBoardName,
                            Rank: i == 0 ? startRank : startRank += counter,
                            x.Country,
                            x.FirstName,
                            x.LeaderBoardName
                        )
                )
                .ToList();
        }

        var sortedsetItems = results.ToList();
        var rankValue = isDesc ? start + 1 : results.Length;

        foreach (var sortedsetItem in sortedsetItems)
        {
            string key = sortedsetItem.Element;
            var playerId = key.Split(":")[1];

            // get detail information about saved sortedset score-player
            var detail = await GetPlayerScoreDetail(
                leaderBoardName,
                key,
                isDesc,
                cancellationToken
            );

            var playerScore = new PlayerScoreDto(
                playerId,
                sortedsetItem.Score,
                leaderBoardName,
                rankValue,
                detail?.Country,
                detail?.FirstName,
                detail?.LastName
            );
            playerScores.Add(playerScore);

            // next rank for next item
            rankValue += counter;
        }

        return playerScores;
    }

    public async Task<PlayerScoreDto?> GetGlobalScoreAndRank(
        string leaderBoardName,
        string playerId,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    )
    {
        string key = $"{nameof(PlayerScore)}:{playerId}";

        // 1. Read data form the cache
        var score = await _redisDatabase.SortedSetScoreAsync(leaderBoardName, playerId);
        var rank = await _redisDatabase.SortedSetRankAsync(
            leaderBoardName,
            playerId,
            isDesc ? Order.Descending : Order.Ascending
        );

        if (score == null || rank == null)
        {
            // 2. If data not exist in the cache, read data from primary database
            var playerScore = await _readProviderDatabase.GetGlobalScoreAndRank(
                leaderBoardName,
                playerId,
                isDesc,
                cancellationToken
            );

            if (playerScore != null)
            {
                // 3. update cache with fetched results from primary database
                await PopulateCache(playerScore);

                return new PlayerScoreDto(
                    playerId,
                    playerScore.Score,
                    leaderBoardName,
                    playerScore.Rank ?? 1,
                    playerScore.Country,
                    playerScore.FirstName,
                    playerScore.LastName
                );
            }

            return null;
        }

        rank = isDesc ? rank + 1 : rank - 1;

        PlayerScoreDetailDto? detail = await GetPlayerScoreDetail(
            leaderBoardName,
            key,
            isDesc,
            cancellationToken
        );

        return new PlayerScoreDto(
            playerId,
            (double)score,
            leaderBoardName,
            rank,
            detail?.Country,
            detail?.FirstName,
            detail?.LastName
        );
    }

    public async Task<List<PlayerScoreDto>?> GetPlayerGroupScoresAndRanks(
        string leaderBoardName,
        IEnumerable<string> playerIds,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    )
    {
        var results = new List<PlayerScoreDto>();
        foreach (var playerId in playerIds)
        {
            var playerScore = await GetGlobalScoreAndRank(
                leaderBoardName,
                playerId,
                isDesc,
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

    private async Task PopulateCache(IQueryable<PlayerScore> databaseQuery)
    {
        await foreach (var playerScore in LoadDatabaseItemByPaging(databaseQuery))
        {
            await PopulateCache(playerScore);
        }
    }

    //https://dev.to/mbernard/asynchronous-streams-in-c-8-0-33la
    private async IAsyncEnumerable<PlayerScore> LoadDatabaseItemByPaging(
        IQueryable<PlayerScore> databaseQuery
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

    private async Task PopulateCache(PlayerScore playerScore)
    {
        var key = $"{nameof(PlayerScore).Underscore()}:{playerScore.PlayerId}";

        // store the summary of our player-score in sortedset
        await _redisDatabase.SortedSetAddAsync(Constants.GlobalLeaderBoard, key, playerScore.Score);

        // store detail of out score-player in hashset. it is related to its score information with their same unique identifier
        await _redisDatabase.HashSetAsync(
            key,
            new HashEntry[]
            {
                new(nameof(PlayerScore.Country).Underscore(), playerScore.Country),
                new(nameof(PlayerScore.FirstName).Underscore(), playerScore.FirstName),
                new(nameof(PlayerScore.LastName).Underscore(), playerScore.LastName),
                new(
                    nameof(PlayerScore.UpdatedAt).Underscore(),
                    playerScore.UpdatedAt.ToString(CultureInfo.InvariantCulture)
                ),
                new(
                    nameof(PlayerScore.CreatedAt).Underscore(),
                    playerScore.CreatedAt.ToString(CultureInfo.InvariantCulture)
                )
            }
        );
    }

    /// <summary>
    ///  Get details information about saved sortedset player score, because sortedset is limited for saving all information
    /// </summary>
    private async Task<PlayerScoreDetailDto?> GetPlayerScoreDetail(
        string leaderboardName,
        string playerIdKey,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    )
    {
        string playerId = playerIdKey.Split(":")[1];
        HashEntry[] item = await _redisDatabase.HashGetAllAsync(playerIdKey);
        if (item.Length == 0)
        {
            var playerScore = await _readProviderDatabase.GetGlobalScoreAndRank(
                leaderboardName,
                playerId,
                isDesc,
                cancellationToken
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
            x => x.Name == nameof(PlayerScore.FirstName).Underscore()
        );
        var lastName = item.SingleOrDefault(
            x => x.Name == nameof(PlayerScore.LastName).Underscore()
        );
        var country = item.SingleOrDefault(x => x.Name == nameof(PlayerScore.Country).Underscore());

        return new PlayerScoreDetailDto(country.Value, firstName.Value, lastName.Value);
    }
}
