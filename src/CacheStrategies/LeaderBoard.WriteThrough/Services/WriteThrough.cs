using System.Globalization;
using Ardalis.GuardClauses;
using AutoMapper;
using Humanizer;
using LeaderBoard.WriteThrough.Dtos;
using LeaderBoard.WriteThrough.Infrastructure.Data.EFContext;
using LeaderBoard.WriteThrough.Models;
using LeaderBoard.WriteThrough.Providers;
using StackExchange.Redis;

namespace LeaderBoard.WriteThrough.Services;

//https://codeahoy.com/2017/08/11/caching-strategies-and-how-to-choose-the-right-one/
//https://www.gomomento.com/blog/6-common-caching-design-patterns-to-execute-your-caching-strategy
//https://www.gomomento.com/blog/3-crucial-caching-choices-where-when-and-how
public class WriteThrough : IWriteThrough
{
    private readonly IMapper _mapper;
    private readonly IWriteProviderDatabase _writeProviderDatabase;
    private readonly LeaderBoardDBContext _leaderBoardDbContext;
    private readonly IDatabase _redisDatabase;

    public WriteThrough(
        IMapper mapper,
        IConnectionMultiplexer redisConnection,
        IWriteProviderDatabase writeProviderDatabase,
        LeaderBoardDBContext leaderBoardDbContext
    )
    {
        _mapper = mapper;
        _writeProviderDatabase = writeProviderDatabase;
        _leaderBoardDbContext = leaderBoardDbContext;
        _redisDatabase = redisConnection.GetDatabase();
    }

    public async Task<bool> AddPlayerScore(
        PlayerScoreDto playerScoreDto,
        CancellationToken cancellationToken = default
    )
    {
        Guard.Against.Null(playerScoreDto);

        var playerScore = _mapper.Map<PlayerScore>(playerScoreDto);

        var key = $"{nameof(PlayerScore).Underscore()}:{playerScore.PlayerId}";
        bool isDesc = true;

        //1. Write data to cache
        // store the summary of our player-score in sortedset
        var res = await _redisDatabase.SortedSetAddAsync(
            playerScore.LeaderBoardName,
            key,
            playerScore.Score
        );
        if (res == false)
            return false;

        var rank = await _redisDatabase.SortedSetRankAsync(playerScore.LeaderBoardName, key);
        rank = isDesc ? rank + 1 : rank - 1;

        // recalculated rank by redis sortedset after adding new item
        playerScore.Rank = rank;

        // store detail of out score-player in hashset. it is related to its score information with their same unique identifier
        await _redisDatabase.HashSetAsync(
            playerScore.PlayerId,
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

        // 2. Write data to primary database
        await _writeProviderDatabase.AddPlayerScore(playerScore, cancellationToken);

        return true;
    }

    public async Task<bool> UpdateScore(
        string leaderBoardName,
        string playerId,
        double value,
        CancellationToken cancellationToken = default
    )
    {
        string key = $"{nameof(PlayerScore)}:{playerId}";
        bool isDesc = true;

        // 1. Write data to cache
        var res = await _redisDatabase.SortedSetAddAsync(leaderBoardName, key, value);
        if (res == false)
            return false;

        var score = await _redisDatabase.SortedSetScoreAsync(leaderBoardName, key);
        var rank = await _redisDatabase.SortedSetRankAsync(leaderBoardName, key);
        rank = isDesc ? rank + 1 : rank - 1;

        // 2. Write data to primary database
        await _writeProviderDatabase.UpdateScore(
            leaderBoardName,
            playerId,
            score ?? 0,
            rank,
            cancellationToken
        );

        return true;
    }
}
