using Ardalis.GuardClauses;
using Humanizer;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;

namespace LeaderBoard.SharedKernel.Redis;

public static class DatabaseExtensions
{
    public static async Task PublishMessage<T>(this IDatabase database, string channelName, T data)
    {
        var jsonData = JsonConvert.SerializeObject(
            data,
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }
        );

        var pubSubMessage = new PubSubMessage { Type = typeof(T).Name, Data = jsonData };

        await database.PublishAsync(channelName, JsonConvert.SerializeObject(pubSubMessage));
    }

    public static async Task PublishMessage<T>(this IDatabase database, T data)
    {
        var channelName = $"{typeof(T).Name.Underscore()}_channel";
        await database.PublishMessage(channelName, data);
    }

    public static async Task SubscribeMessage<T>(
        this IDatabase database,
        string channelName,
        Action<string, T> handler
    )
    {
        await database.Multiplexer
            .GetSubscriber()
            .SubscribeAsync(
                channelName,
                (chan, val) =>
                {
                    Guard.Against.NullOrEmpty(chan);
                    Guard.Against.NullOrEmpty(val);

                    var message = JsonConvert.DeserializeObject<T>(val!);
                    handler(chan!, message!);
                }
            );
    }

    public static async Task SubscribeMessage<T>(this IDatabase database, Action<string, T> handler)
    {
        var channelName = $"{typeof(T).Name.Underscore()}_channel";

        await database.SubscribeMessage(channelName, handler);
    }
}
