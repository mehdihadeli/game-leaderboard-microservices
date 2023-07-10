using LeaderBoard.Dtos;
using LeaderBoard.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LeaderBoard.Services;

public class PlayerScoreService : IPlayerScoreService
{
    private readonly LeaderBoardOptions _leaderboardOptions;
    private readonly IDatabase _redisDatabase;

    public PlayerScoreService(
        IConnectionMultiplexer redisConnection,
        IOptions<LeaderBoardOptions> leaderboardOptions
    )
    {
        _leaderboardOptions = leaderboardOptions.Value;
        _redisDatabase = redisConnection.GetDatabase();
    }

    public async Task<bool> AddOrUpdateScore(string leaderBoardName, string playerId, double value)
    {
        var res = await _redisDatabase.SortedSetAddAsync(leaderBoardName, playerId, value);
        if (res == false)
            return false;
        var detail = await GetPlayerScoreDetail(playerId);
        var score = await _redisDatabase.SortedSetScoreAsync(leaderBoardName, playerId);
        var rank = await _redisDatabase.SortedSetRankAsync(leaderBoardName, playerId);

        HashEntry[] hashEntries =
        {
            new(nameof(PlayerScore.Score).ToLower(), score),
            new(nameof(PlayerScore.Rank).ToLower(), rank),
            new(nameof(PlayerScore.FirstName).ToLower(), detail.FirstName),
            new(nameof(PlayerScore.LastName).ToLower(), detail.LastName),
            new(nameof(PlayerScore.Country).ToLower(), detail.Country),
        };

        if (_leaderboardOptions.UseWriteBehind)
        {
            // write behind strategy - will handle by caching provider(like redis gears) internally or out external library or azure function or background services
            // publish a stream to redis
            _redisDatabase.StreamAdd(
                GetStreamName(playerId),
                hashEntries.Select(x => new NameValueEntry(x.Name, x.Value)).ToArray()
            );
            // Or publish message to broker
        }

        return true;
    }

    private string GetStreamName(string key)
    {
        return $"_score_player-stream-{key}";
    }

    public async Task<List<PlayerScoreDto>> GetScoresAndRanks(
        string leaderBoardName,
        int start,
        int ent,
        bool isDesc = true
    )
    {
        var playerScores = new List<PlayerScoreDto>();
        var results = await _redisDatabase.SortedSetRangeByRankWithScoresAsync(
            leaderBoardName,
            start,
            ent,
            isDesc ? Order.Descending : Order.Ascending
        );

        var items = results.ToList();
        var rankValue = isDesc ? start + 1 : results.Length;
        var counter = isDesc ? 1 : -1;

        foreach (var t in items)
        {
            string key = t.Element;

            // get detail information about saved sortedset score-player
            var detail = await GetPlayerScoreDetail(key);

            var playerScore = new PlayerScoreDto(
                key,
                t.Score,
                rankValue,
                detail.Country,
                detail.FirstName,
                detail.LastName
            );
            playerScores.Add(playerScore);

            // next rank for next item
            rankValue += counter;
        }

        return playerScores;
    }

    public async Task<PlayerScoreDetailDto> GetPlayerScoreDetail(string playerId)
    {
        HashEntry[] item = await _redisDatabase.HashGetAllAsync(playerId);
        var firstName = item.SingleOrDefault(
            x => x.Name == nameof(PlayerScore.FirstName).ToLower()
        );
        var lastName = item.SingleOrDefault(x => x.Name == nameof(PlayerScore.LastName).ToLower());
        var country = item.SingleOrDefault(x => x.Name == nameof(PlayerScore.Country).ToLower());

        return new PlayerScoreDetailDto(country.Value, firstName.Value, lastName.Value);
    }

    public async Task<PlayerScoreDto> GetScoreAndRank(
        string leaderBoardName,
        string playerId,
        bool isDesc = true
    )
    {
        var score = await _redisDatabase.SortedSetScoreAsync(leaderBoardName, playerId);
        var detail = await GetPlayerScoreDetail(playerId);
        var rank = await _redisDatabase.SortedSetRankAsync(
            leaderBoardName,
            playerId,
            isDesc ? Order.Descending : Order.Ascending
        );

        rank = isDesc ? rank + 1 : rank - 1;

        return new PlayerScoreDto(
            playerId,
            score ?? 0,
            rank ?? 1,
            detail.Country,
            detail.FirstName,
            detail.LastName
        );
    }
}
