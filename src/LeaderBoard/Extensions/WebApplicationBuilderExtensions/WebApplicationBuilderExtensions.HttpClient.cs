using LeaderBoard.Infrastructure.Clients;
using LeaderBoard.SharedKernel.Core.Extensions.ServiceCollectionExtensions;
using Microsoft.Extensions.Options;

namespace LeaderBoard.Extensions.WebApplicationBuilderExtensions;

public static partial class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddReadThroughClient(this WebApplicationBuilder builder)
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
}
