using Humanizer;
using LeaderBoard.ReadThrough.PlayerScores.Dtos;
using LeaderBoard.ReadThrough.Shared.Providers;
using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.SharedKernel.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace LeaderBoard.ReadThrough.Shared.Services;

//https://codeahoy.com/2017/08/11/caching-strategies-and-how-to-choose-the-right-one/
//https://www.gomomento.com/blog/6-common-caching-design-patterns-to-execute-your-caching-strategy
//https://www.gomomento.com/blog/3-crucial-caching-choices-where-when-and-how
public class ReadThrough : IReadThrough
{
    private readonly IReadProviderDatabase _postgresReadProviderDatabase;
    private readonly IDatabase _redisDatabase;

    public ReadThrough(IConnectionMultiplexer redisConnection, IReadProviderDatabase postgresReadProviderDatabase)
    {
        _postgresReadProviderDatabase = postgresReadProviderDatabase;
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
        var counter = 1;
        var playerScores = new List<PlayerScoreDto>();

        // 1. Read data form the cache
        var results = await _redisDatabase.SortedSetRangeByRankWithScoresAsync(
            leaderBoardName,
            start,
            end,
            isDesc ? Order.Descending : Order.Ascending
        );

        var startRank = start + 1;

        if (results == null || results.Length == 0)
        {
            // 2. If data not exist in the cache, read data from primary database
            var items = _postgresReadProviderDatabase.GetScoresAndRanks(leaderBoardName, isDesc, cancellationToken);

            // 3. update cache with fetched results from primary database
            await PopulateCache(items);
            var data = await items.Skip(start).Take(end + 1).ToListAsync(cancellationToken: cancellationToken);

            if (data.Count == 0)
            {
                return new List<PlayerScoreDto>();
            }

            return data.Select(
                    (x, i) =>
                        new PlayerScoreDto(
                            x.PlayerId,
                            x.Score,
                            leaderBoardName,
                            Rank: i == 0 ? startRank : startRank += counter,
                            x.FirstName,
                            x.LastName,
                            x.Country
                        )
                )
                .ToList();
        }

        foreach (var sortedsetItem in results)
        {
            string key = sortedsetItem.Element;
            var playerId = key.Split(":")[1];

            // get detail information about saved sortedset score-player
            var detail = await GetPlayerScoreDetail(leaderBoardName, key, isDesc, cancellationToken);

            var playerScore = new PlayerScoreDto(
                playerId,
                sortedsetItem.Score,
                leaderBoardName,
                startRank,
                detail?.FirstName,
                detail?.LastName,
                detail?.Country
            );
            playerScores.Add(playerScore);

            // next rank for next item
            startRank += counter;
        }

        return playerScores;
    }

    public async Task<PlayerScoreWithNeighborsDto?> GetGlobalScoreAndRank(
        string leaderBoardName,
        string playerId,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    )
    {
        string key = $"{nameof(PlayerScoreReadModel).Underscore()}:{playerId}";

        // 1. Read data form the cache
        var score = await _redisDatabase.SortedSetScoreAsync(leaderBoardName, playerId);
        var rank = await _redisDatabase.SortedSetRankAsync(
            leaderBoardName,
            playerId,
            isDesc ? Order.Descending : Order.Ascending
        );

        if (score == null || rank == null)
        {
            // 2. If data not exist in the cache, first read data from primary database
            var playerScore = await _postgresReadProviderDatabase.GetGlobalScoreAndRank(
                leaderBoardName,
                playerId,
                isDesc,
                cancellationToken
            );

            if (playerScore != null)
            {
                // 3. update cache with fetched results from primary database
                await PopulateCache(playerScore);

                rank = await _redisDatabase.SortedSetRankAsync(
                    leaderBoardName,
                    key,
                    isDesc ? Order.Descending : Order.Ascending
                );
                rank = isDesc ? rank + 1 : rank - 1;

                var nextMember = await GetNextMember(leaderBoardName, key, isDesc);
                var previousMember = await GetPreviousMember(leaderBoardName, key, isDesc);

                return new PlayerScoreWithNeighborsDto(
                    previousMember,
                    new PlayerScoreDto(
                        playerId,
                        playerScore.Score,
                        leaderBoardName,
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
            PlayerScoreDetailDto? detail = await GetPlayerScoreDetail(leaderBoardName, key, isDesc, cancellationToken);

            var nextMember = await GetNextMember(leaderBoardName, key, isDesc);
            var previousMember = await GetPreviousMember(leaderBoardName, key, isDesc);

            rank = isDesc ? rank + 1 : rank - 1;

            return new PlayerScoreWithNeighborsDto(
                previousMember,
                new PlayerScoreDto(
                    playerId,
                    (double)score,
                    leaderBoardName,
                    rank,
                    detail?.FirstName,
                    detail?.LastName,
                    detail?.Country
                ),
                nextMember
            );
        }
    }

    public async Task<List<PlayerScoreWithNeighborsDto>?> GetPlayerGroupGlobalScoresAndRanks(
        string leaderBoardName,
        IEnumerable<string> playerIds,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    )
    {
        var results = new List<PlayerScoreWithNeighborsDto>();
        foreach (var playerId in playerIds)
        {
            var playerScore = await GetGlobalScoreAndRank(leaderBoardName, playerId, isDesc, cancellationToken);
            if (playerScore != null)
            {
                results.Add(playerScore);
            }
        }

        List<PlayerScoreWithNeighborsDto> items = isDesc
            ? results.OrderByDescending(x => x.CurrentPlayerScore.Score).ToList()
            : results.OrderBy(x => x.CurrentPlayerScore.Score).ToList();

        return items;
    }

    private async Task PopulateCache(IQueryable<PlayerScoreReadModel> databaseQuery)
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

    private async Task PopulateCache(PlayerScoreReadModel playerScore)
    {
        var key = $"{nameof(PlayerScoreReadModel).Underscore()}:{playerScore.PlayerId}";

        // store the summary of our player-score in sortedset
        await _redisDatabase.SortedSetAddAsync(Constants.GlobalLeaderBoard, key, playerScore.Score);

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
            var playerScore = await _postgresReadProviderDatabase.GetGlobalScoreAndRank(
                leaderboardName,
                playerId,
                isDesc,
                cancellationToken
            );
            if (playerScore != null)
            {
                await PopulateCache(playerScore);
                return new PlayerScoreDetailDto(playerScore.Country, playerScore.FirstName, playerScore.LastName);
            }

            return null;
        }

        var firstName = item.SingleOrDefault(x => x.Name == nameof(PlayerScoreReadModel.FirstName).Underscore());
        var lastName = item.SingleOrDefault(x => x.Name == nameof(PlayerScoreReadModel.LastName).Underscore());
        var country = item.SingleOrDefault(x => x.Name == nameof(PlayerScoreReadModel.Country).Underscore());

        return new PlayerScoreDetailDto(country.Value, firstName.Value, lastName.Value);
    }

    public async Task<PlayerScoreDto?> GetNextMemberByRank(string leaderBoardName, long rank, bool isDesc = true)
    {
        var counter = isDesc ? 1 : -1;
        var lastRank = _redisDatabase.SortedSetLength(leaderBoardName) - 1;

        // if our current rank is last rank in the redis sortedset, the next member will be null
        if (rank == lastRank)
            return null;

        var results = await _redisDatabase.SortedSetRangeByRankWithScoresAsync(
            leaderBoardName,
            start: rank + 1, // next member
            rank + 1, // next member
            isDesc ? Order.Descending : Order.Ascending
        );

        if (results.Length == 0)
            return null;

        var sortedsetItem = results.FirstOrDefault();

        string key = sortedsetItem.Element;
        var playerId = key.Split(":")[1];

        // get detail information about saved sortedset score-player
        var detail = await GetPlayerScoreDetail(leaderBoardName, key, isDesc);

        var startRank = isDesc ? rank + 1 : rank - 1;
        var nextRank = startRank + counter;

        var playerScore = new PlayerScoreDto(
            playerId,
            sortedsetItem.Score,
            leaderBoardName,
            nextRank,
            detail?.FirstName ?? string.Empty,
            detail?.LastName ?? string.Empty,
            detail?.Country ?? string.Empty
        );

        return playerScore;
    }

    public async Task<PlayerScoreDto?> GetNextMember(string leaderBoardName, string memberKey, bool isDesc = true)
    {
        var counter = isDesc ? 1 : -1;
        var lastRank = _redisDatabase.SortedSetLength(leaderBoardName) - 1;

        var rank = await _redisDatabase.SortedSetRankAsync(
            leaderBoardName,
            memberKey,
            isDesc ? Order.Descending : Order.Ascending
        );
        if (rank is null)
            throw new NotFoundException($"Could not find rank for {memberKey} member");

        // if our current rank is last rank in the redis sortedset, the next member will be null
        if (rank == lastRank)
            return null;

        var results = await _redisDatabase.SortedSetRangeByRankWithScoresAsync(
            leaderBoardName,
            start: (long)rank + 1, // next member
            (long)rank + 1, // next member
            isDesc ? Order.Descending : Order.Ascending
        );

        if (results.Length == 0)
            return null;

        var sortedsetItem = results.FirstOrDefault();

        string key = sortedsetItem.Element;
        var playerId = key.Split(":")[1];

        // get detail information about saved sortedset score-player
        var detail = await GetPlayerScoreDetail(leaderBoardName, key, isDesc);

        var startRank = isDesc ? rank + 1 : rank - 1;
        var nextRank = startRank + counter;

        var playerScore = new PlayerScoreDto(
            playerId,
            sortedsetItem.Score,
            leaderBoardName,
            nextRank,
            detail?.FirstName ?? string.Empty,
            detail?.LastName ?? string.Empty,
            detail?.Country ?? string.Empty
        );

        return playerScore;
    }

    public async Task<PlayerScoreDto?> GetPreviousMember(string leaderBoardName, string memberKey, bool isDesc = true)
    {
        var counter = isDesc ? 1 : -1;

        var rank = await _redisDatabase.SortedSetRankAsync(
            leaderBoardName,
            memberKey,
            isDesc ? Order.Descending : Order.Ascending
        );
        if (rank is null)
            throw new NotFoundException($"Could not find rank for {memberKey} member");

        // if our current rank is lower possible redis rank, previous element is null
        if (rank == 0)
            return null;

        var results = await _redisDatabase.SortedSetRangeByRankWithScoresAsync(
            leaderBoardName,
            start: (long)rank - 1, // previous member
            (long)rank - 1, // previous member
            isDesc ? Order.Descending : Order.Ascending
        );

        if (results.Length == 0)
            return null;

        var sortedsetItem = results.FirstOrDefault();

        string key = sortedsetItem.Element;
        var playerId = key.Split(":")[1];

        // get detail information about saved sortedset score-player
        var detail = await GetPlayerScoreDetail(leaderBoardName, key, isDesc);

        var startRank = isDesc ? rank + 1 : rank - 1;

        var previousRank = startRank - counter;

        var playerScore = new PlayerScoreDto(
            playerId,
            sortedsetItem.Score,
            leaderBoardName,
            previousRank,
            detail?.FirstName ?? string.Empty,
            detail?.LastName ?? string.Empty,
            detail?.Country ?? string.Empty
        );

        return playerScore;
    }

    public async Task<PlayerScoreDto?> GetPreviousMemberByRank(string leaderBoardName, long rank, bool isDesc = true)
    {
        var counter = isDesc ? 1 : -1;

        // if our current rank is lower possible redis rank, previous element is null
        if (rank == 0)
            return null;

        var results = await _redisDatabase.SortedSetRangeByRankWithScoresAsync(
            leaderBoardName,
            start: rank - 1, // previous member
            rank - 1, // previous member
            isDesc ? Order.Descending : Order.Ascending
        );

        if (results.Length == 0)
            return null;

        var sortedsetItem = results.FirstOrDefault();

        string key = sortedsetItem.Element;
        var playerId = key.Split(":")[1];

        // get detail information about saved sortedset score-player
        var detail = await GetPlayerScoreDetail(leaderBoardName, key, isDesc);

        var startRank = isDesc ? rank + 1 : rank - 1;
        var nextRank = startRank + counter;

        var playerScore = new PlayerScoreDto(
            playerId,
            sortedsetItem.Score,
            leaderBoardName,
            nextRank,
            detail?.FirstName ?? string.Empty,
            detail?.LastName ?? string.Empty,
            detail?.Country ?? string.Empty
        );

        return playerScore;
    }
}
