using LeaderBoard.SharedKernel.Core.Extensions.ServiceCollectionExtensions;
using LeaderBoard.SignalR.Clients.GameEventProcessor;
using Microsoft.Extensions.Options;

namespace LeaderBoard.SignalR.Extensions.WebApplicationBuilderExtensions;

public static partial class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddCustomHttpClients(this WebApplicationBuilder builder)
    {
        builder.AddGameEventProcessorClient();

        return builder;
    }

    private static WebApplicationBuilder AddGameEventProcessorClient(
        this WebApplicationBuilder builder
    )
    {
        builder.Services.AddValidatedOptions<GameEventProcessorClientOptions>();

        builder.Services
            .AddHttpClient<IGameEventProcessorClient, GameEventProcessorClient>()
            .ConfigureHttpClient(
                (sp, httpClient) =>
                {
                    var httpClientOptions = sp.GetRequiredService<
                        IOptions<GameEventProcessorClientOptions>
                    >().Value;
                    httpClient.BaseAddress = new Uri(httpClientOptions.BaseAddress);
                    httpClient.Timeout = TimeSpan.FromSeconds(httpClientOptions.Timeout);
                }
            );

        return builder;
    }
}
