using Humanizer;
using LeaderBoard.GameEventsProcessor.PlayerScores.Dtos;
using LeaderBoard.GameEventsProcessor.Shared;
using LeaderBoard.GameEventsProcessor.Shared.Clients.WriteThrough;
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
                request.LeaderBoardName,
                request.Country
            );
        }
        else
        {
            // update existing aggregate
            playerScore.Update(request.Score, request.FirstName, request.LastName, request.Country);
        }

        if (_leaderBoardOptions.UseWriteCacheAside)
        {
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
                    playerScore.Id,
                    playerScore.Score,
                    playerScore.LeaderBoardName,
                    null,
                    playerScore.FirstName,
                    playerScore.LastName,
                    playerScore.Country
                ),
                cancellationToken
            );
        }
        else if (_leaderBoardOptions.UseWriteBehind)
        {
            // https://codeahoy.com/2017/08/11/caching-strategies-and-how-to-choose-the-right-one/
            // write-behind: the application writes data to the cache which stores the data and acknowledges to the application immediately. Then later, the cache writes the data back to the database asynchronously.
            HashEntry[] hashEntries =
            {
                new(nameof(PlayerScoreReadModel.PlayerId).Underscore(), playerScore.Id),
                new(
                    nameof(PlayerScoreReadModel.LeaderBoardName).Underscore(),
                    playerScore.LeaderBoardName
                ),
                new(nameof(PlayerScoreReadModel.Score).Underscore(), playerScore.Score),
                new(nameof(PlayerScoreReadModel.FirstName).Underscore(), playerScore?.FirstName),
                new(nameof(PlayerScoreReadModel.LastName).Underscore(), playerScore?.LastName),
                new(nameof(PlayerScoreReadModel.Country).Underscore(), playerScore?.Country),
            };

            var playerScoreAdded = new PlayerScoreAddOrUpdated(
                playerScore!.Id,
                playerScore.Score,
                playerScore.LeaderBoardName,
                playerScore.Country,
                playerScore.FirstName,
                playerScore.LastName
            );

            // write behind strategy - will handle by caching provider(like redis gears) internally or out external library or azure function or background services
            // redis pub/sub
            // uses a CommandChannelValueMessage message internally on top of redis stream
            await _redisDatabase.PublishMessage(playerScoreAdded);

            // publish a stream to redis (both PublishMessage and StreamAddAsync use same streaming mechanism behind the scenes)
            // uses a CommandKeyValuesMessage message internally on top of redis stream
            await _redisDatabase.StreamAddAsync(
                GetStreamName<PlayerScoreAddOrUpdated>(playerScore.Id), //_player_score_added-stream-{key}
                hashEntries.Select(x => new NameValueEntry(x.Name, x.Value)).ToArray()
            );

            // Or publish message to broker
            await _busPublisher.Publish(playerScoreAdded, cancellationToken);
        }
        // just update the cache
        else
        {
            var key = $"{nameof(PlayerScoreReadModel).Underscore()}:{playerScore.Id}";
            bool isDesc = true;

            bool exists = true;

            var currentScore = _redisDatabase.SortedSetScore(playerScore.LeaderBoardName, key);
            if (currentScore == null)
            {
                exists = false;
            }

            // https://stackoverflow.com/questions/25976231/stackexchange-redis-transaction-methods-freezes
            // https://github.com/olsh/stack-exchange-redis-analyzer
            var transaction = _redisDatabase.CreateTransaction();

            // increment score and calculation rank in redis
            var newScoreTask = transaction.SortedSetIncrementAsync(
                playerScore.LeaderBoardName,
                key,
                playerScore.Score
            );

            // because ranks will change very fast between players, storing it in primary database is useless
            var rankTask = transaction.SortedSetRankAsync(
                playerScore.LeaderBoardName,
                key,
                isDesc ? Order.Descending : Order.Ascending
            );

            // store detail of out score-player in hashset. it is related to its score information with their same unique identifier
            var hashsetTask = transaction.HashSetAsync(
                playerScore.Id,
                new HashEntry[]
                {
                    new(nameof(PlayerScoreReadModel.Country).Underscore(), playerScore.Country),
                    new(nameof(PlayerScoreReadModel.FirstName).Underscore(), playerScore.FirstName),
                    new(nameof(PlayerScoreReadModel.LastName).Underscore(), playerScore.LastName),
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
