using Humanizer;
using LeaderBoard.GameEventsProcessor.PlayerScores.Dtos;
using LeaderBoard.GameEventsProcessor.Shared;
using LeaderBoard.GameEventsProcessor.Shared.Clients.WriteThrough;
using LeaderBoard.GameEventsProcessor.Shared.LocalRedisMessage;
using LeaderBoard.SharedKernel.Application.Messages.PlayerScore;
using LeaderBoard.SharedKernel.Application.Models;
using LeaderBoard.SharedKernel.Bus;
using LeaderBoard.SharedKernel.Contracts.Data.EventStore;
using LeaderBoard.SharedKernel.Redis;
using MediatR;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LeaderBoard.GameEventsProcessor.PlayerScores.Features.AddingOrUpdatingPlayerScore;

public record AddOrUpdatePlayerScore(
    Guid Id,
    double Score,
    string LeaderBoardName,
    string FirstName,
    string LastName,
    string Country
) : IRequest;

internal class AddOrUpdatePlayerScoreHandler : IRequestHandler<AddOrUpdatePlayerScore>
{
    private readonly IAggregateStore _aggregateStore;
    private readonly IBusPublisher _busPublisher;
    private readonly IWriteThroughClient _writeThroughClient;
    private readonly LeaderBoardOptions _leaderBoardOptions;
    private readonly IDatabase _redisDatabase;

    public AddOrUpdatePlayerScoreHandler(
        IAggregateStore aggregateStore,
        IBusPublisher busPublisher,
        IWriteThroughClient writeThroughClient,
        IConnectionMultiplexer redisConnection,
        IOptions<LeaderBoardOptions> leaderBoardOptions
    )
    {
        _aggregateStore = aggregateStore;
        _busPublisher = busPublisher;
        _writeThroughClient = writeThroughClient;
        _leaderBoardOptions = leaderBoardOptions.Value;
        _redisDatabase = redisConnection.GetDatabase();
    }

    public async Task Handle(AddOrUpdatePlayerScore request, CancellationToken cancellationToken)
    {
        if (_leaderBoardOptions.UseWriteCacheAside)
        {
            var playerScore = await _aggregateStore.GetAsync<PlayerScoreAggregate, string>(
                request.Id.ToString(),
                cancellationToken
            );

            if (playerScore is null)
            {
                // create a new aggregate
                playerScore = PlayerScoreAggregate.Create(
                    request.Id.ToString(),
                    request.Score,
                    request.LeaderBoardName,
                    request.FirstName,
                    request.LastName,
                    request.Country
                );
            }
            else
            {
                // update existing aggregate
                playerScore.Update(request.Score, request.FirstName, request.LastName, request.Country);
            }
            //https://www.gomomento.com/blog/6-common-caching-design-patterns-to-execute-your-caching-strategy
            // write-aside caching: rather than lazily loading items into our cache after accessing it for the first time, we are proactively pushing data to our cache when we write it.
            var appendResult = await _aggregateStore.StoreAsync<PlayerScoreAggregate, string>(
                playerScore,
                cancellationToken
            );
        }
        else if (_leaderBoardOptions.UseWriteThrough)
        {
            // https://codeahoy.com/2017/08/11/caching-strategies-and-how-to-choose-the-right-one/
            // write-through: In this write strategy, data is first written to the cache and then to the database. The cache sits in-line with the database and writes always go through the cache to the main database. This
            await _writeThroughClient.AddOrUpdatePlayerScore(
                new PlayerScoreDto(
                    request.Id.ToString(),
                    request.Score,
                    request.LeaderBoardName,
                    null,
                    request.FirstName,
                    request.LastName,
                    request.Country
                ),
                cancellationToken
            );
        }
        else if (_leaderBoardOptions.UseWriteBehind)
        {
            // 1.first update cache
            var key = $"{nameof(PlayerScoreReadModel).Underscore()}:{request.Id}";
            bool isDesc = true;

            bool exists = true;

            var currentScore = _redisDatabase.SortedSetScore(request.LeaderBoardName, key);
            if (currentScore == null)
            {
                exists = false;
            }

            // https://stackoverflow.com/questions/25976231/stackexchange-redis-transaction-methods-freezes
            // https://github.com/olsh/stack-exchange-redis-analyzer
            var transaction = _redisDatabase.CreateTransaction();

            // increment score and calculation rank in redis
            var newScoreTask = transaction.SortedSetIncrementAsync(request.LeaderBoardName, key, request.Score);

            // because ranks will change very fast between players, storing it in primary database is useless
            var rankTask = transaction.SortedSetRankAsync(
                request.LeaderBoardName,
                key,
                isDesc ? Order.Descending : Order.Ascending
            );

            // store detail of out score-player in hashset. it is related to its score information with their same unique identifier
            var hashsetTask = transaction.HashSetAsync(
                key,
                new HashEntry[]
                {
                    new(nameof(PlayerScoreReadModel.PlayerId).Underscore(), request.Id.ToString()),
                    new(nameof(PlayerScoreReadModel.LeaderBoardName).Underscore(), request.LeaderBoardName),
                    new(nameof(PlayerScoreReadModel.Score).Underscore(), request.Score),
                    new(nameof(PlayerScoreReadModel.Country).Underscore(), request.Country),
                    new(nameof(PlayerScoreReadModel.FirstName).Underscore(), request.FirstName),
                    new(nameof(PlayerScoreReadModel.LastName).Underscore(), request.LastName),
                }
            );

            // 2.update main database
            // https://codeahoy.com/2017/08/11/caching-strategies-and-how-to-choose-the-right-one/
            // write-behind: the application writes data to the cache which stores the data and acknowledges to the application immediately. Then later, the cache writes the data back to the database asynchronously.

            // // write behind strategy - will handle by caching provider(like redis gears) internally or out external library or azure function or background services
            // // redis pub/sub
            // // uses a CommandChannelValueMessage message internally on top of redis stream
            // await _redisDatabase.PublishMessage(playerScoreAdded);
            //
            // // publish a stream to redis (both PublishMessage and StreamAddAsync use same streaming mechanism behind the scenes)
            // // uses a CommandKeyValuesMessage message internally on top of redis stream
            // await _redisDatabase.StreamAddAsync(
            //     GetStreamName<PlayerScoreAddOrUpdated>(request.Id.ToString()), //_player_score_added-stream-{key}
            //     hashEntries.Select(x => new NameValueEntry(x.Name, x.Value)).ToArray()
            // );

            // Or publish message to broker
            var publishRedisLocalAddOrUpdatePlayer = transaction.PublishMessage(
                RedisLocalAddOrUpdatePlayerMessage.ChannelName,
                new RedisLocalAddOrUpdatePlayerMessage(
                    request.Id.ToString(),
                    request.Score,
                    request.LeaderBoardName,
                    request.FirstName,
                    request.LastName,
                    request.Country
                )
            );

            if (await transaction.ExecuteAsync())
            {
                var newScoreValue = await newScoreTask;
                var rank = await rankTask;
                rank = isDesc ? rank + 1 : rank - 1;

                await hashsetTask;
                await publishRedisLocalAddOrUpdatePlayer;
            }
        }
        // just update the cache
        else
        {
            var key = $"{nameof(PlayerScoreReadModel).Underscore()}:{request.Id}";
            bool isDesc = true;

            bool exists = true;

            var currentScore = _redisDatabase.SortedSetScore(request.LeaderBoardName, key);
            if (currentScore == null)
            {
                exists = false;
            }

            // https://stackoverflow.com/questions/25976231/stackexchange-redis-transaction-methods-freezes
            // https://github.com/olsh/stack-exchange-redis-analyzer
            var transaction = _redisDatabase.CreateTransaction();

            // increment score and calculation rank in redis
            var newScoreTask = transaction.SortedSetIncrementAsync(request.LeaderBoardName, key, request.Score);

            // because ranks will change very fast between players, storing it in primary database is useless
            var rankTask = transaction.SortedSetRankAsync(
                request.LeaderBoardName,
                key,
                isDesc ? Order.Descending : Order.Ascending
            );

            // store detail of out score-player in hashset. it is related to its score information with their same unique identifier
            var hashsetTask = transaction.HashSetAsync(
                key,
                new HashEntry[]
                {
                    new(nameof(PlayerScoreReadModel.Country).Underscore(), request.Country),
                    new(nameof(PlayerScoreReadModel.FirstName).Underscore(), request.FirstName),
                    new(nameof(PlayerScoreReadModel.LastName).Underscore(), request.LastName),
                }
            );

            if (await transaction.ExecuteAsync())
            {
                var newScoreValue = await newScoreTask;
                var rank = await rankTask;
                rank = isDesc ? rank + 1 : rank - 1;

                await hashsetTask;
            }
        }
    }

    private string GetStreamName(string messageType, string key)
    {
        return $"_{messageType}-stream-{key}";
    }

    private string GetStreamName<T>(string key)
    {
        return $"_{typeof(T).Name.Underscore()}-stream-{key}";
    }
}
