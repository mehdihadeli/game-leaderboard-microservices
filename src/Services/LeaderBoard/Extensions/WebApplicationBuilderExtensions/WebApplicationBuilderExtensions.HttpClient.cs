using LeaderBoard.Infrastructure.Clients;
using LeaderBoard.Infrastructure.Clients.WriteThrough;
using LeaderBoard.SharedKernel.Core.Extensions.ServiceCollectionExtensions;
using Microsoft.Extensions.Options;

namespace LeaderBoard.Extensions.WebApplicationBuilderExtensions;

public static partial class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddCustomHttpClients(this WebApplicationBuilder builder)
    {
        builder.AddReadThroughClient();
        builder.AddWriteThroughClient();

        return builder;
    }

    private static WebApplicationBuilder AddReadThroughClient(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatedOptions<ReadThroughHttpClientOptions>();

        builder.Services
            .AddHttpClient<IReadThroughClient, ReadThroughClient>()
            .ConfigureHttpClient(
                (sp, httpClient) =>
                {
                    var httpClientOptions = sp.GetRequiredService<
                        IOptions<ReadThroughHttpClientOptions>
                    >().Value;
                    httpClient.BaseAddress = new Uri(httpClientOptions.BaseAddress);
                    httpClient.Timeout = TimeSpan.FromSeconds(httpClientOptions.Timeout);
                }
            );

        return builder;
    }

    private static WebApplicationBuilder AddWriteThroughClient(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatedOptions<WriteThroughHttpClientOptions>();

        builder.Services
            .AddHttpClient<IWriteThroughClient, WriteThroughClient>()
            .ConfigureHttpClient(
                (sp, httpClient) =>
                {
                    var httpClientOptions = sp.GetRequiredService<
                        IOptions<WriteThroughHttpClientOptions>
                    >().Value;
                    httpClient.BaseAddress = new Uri(httpClientOptions.BaseAddress);
                    httpClient.Timeout = TimeSpan.FromSeconds(httpClientOptions.Timeout);
                }
            );

        return builder;
    }
}
