using AutoMapper;
using Humanizer;
using LeaderBoard.SharedKernel.Application.Messages.PlayerScore;
using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.SharedKernel.Redis;
using LeaderBoard.WriteThrough.PlayerScore.Dtos;
using LeaderBoard.WriteThrough.Shared.LocalRedisMessages;
using LeaderBoard.WriteThrough.Shared.Providers;
using StackExchange.Redis;

namespace LeaderBoard.WriteThrough.Shared.Services;

//https://codeahoy.com/2017/08/11/caching-strategies-and-how-to-choose-the-right-one/
//https://www.gomomento.com/blog/6-common-caching-design-patterns-to-execute-your-caching-strategy
//https://www.gomomento.com/blog/3-crucial-caching-choices-where-when-and-how
public class WriteThrough : IWriteThrough
{
    private readonly IDatabase _redisDatabase;

    public WriteThrough(IConnectionMultiplexer redisConnection)
    {
        _redisDatabase = redisConnection.GetDatabase();
    }

    public async Task AddOrUpdatePlayerScore(
        PlayerScoreDto playerScoreDto,
        CancellationToken cancellationToken = default
    )
    {
        // Write-Through: In write-through strategy, data is first written to the cache and then to the database. The cache sits in-line with the database and writes always go through the cache to the main database.
        var key = $"{nameof(PlayerScoreReadModel).Underscore()}:{playerScoreDto.PlayerId}";
        bool isDesc = true;

        bool exists = true;

        var currentScoreTask = _redisDatabase.SortedSetScoreAsync(
            playerScoreDto.LeaderBoardName,
            key
        );

        var currentScore = await currentScoreTask;
        if (currentScore == null)
        {
            exists = false;
        }

        // https://stackoverflow.com/questions/25976231/stackexchange-redis-transaction-methods-freezes
        // https://github.com/olsh/stack-exchange-redis-analyzer
        var redisTransaction = _redisDatabase.CreateTransaction();

        // phase 1: update cache
        // increment score and calculation rank in redis
        var newScoreTask = redisTransaction.SortedSetIncrementAsync(
            playerScoreDto.LeaderBoardName,
            key,
            playerScoreDto.Score
        );

        // because ranks will change very fast between players, storing it in primary database is useless
        var rankTask = redisTransaction.SortedSetRankAsync(
            playerScoreDto.LeaderBoardName,
            key,
            isDesc ? Order.Descending : Order.Ascending
        );

        // store detail of out score-player in hashset. it is related to its score information with their same unique identifier
        var hashsetTask = redisTransaction.HashSetAsync(
            key,
            new HashEntry[]
            {
                new(nameof(PlayerScoreReadModel.Country).Underscore(), playerScoreDto.Country),
                new(nameof(PlayerScoreReadModel.FirstName).Underscore(), playerScoreDto.FirstName),
                new(nameof(PlayerScoreReadModel.LastName).Underscore(), playerScoreDto.LastName),
            }
        );

        // phase 2: update main database through a send event to cache and subscribe to it
        // add to redis stream, to update primary database after writing to redis
        var publishScoreAddOrUpdatedTask = redisTransaction.PublishMessage(
            RedisLocalAddOrUpdatePlayerMessage.ChannelName,
            new RedisLocalAddOrUpdatePlayerMessage(
                playerScoreDto!.PlayerId,
                playerScoreDto.Score,
                playerScoreDto.LeaderBoardName,
                playerScoreDto.FirstName,
                playerScoreDto.LastName,
                playerScoreDto.Country
            )
        );

        // Send a message to update ui with signalr
        double updatedScore = (currentScore ?? 0) + playerScoreDto.Score;
        double previousScore = currentScore ?? 0;
        var publishScoreChangedTask = redisTransaction.PublishMessage(
            RedisScoreChangedMessage.ChannelName,
            new RedisScoreChangedMessage(
                playerScoreDto.PlayerId,
                playerScoreDto.LeaderBoardName,
                previousScore,
                updatedScore,
                isDesc
            )
        );

        if (await redisTransaction.ExecuteAsync())
        {
            var newScoreValue = await newScoreTask;
            var rank = await rankTask;
            rank = isDesc ? rank + 1 : rank - 1;

            await hashsetTask;
            await publishScoreAddOrUpdatedTask;
            await publishScoreChangedTask;
        }
    }
}
