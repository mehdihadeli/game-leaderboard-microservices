using AutoMapper;
using LeaderBoard.Dtos;
using LeaderBoard.Infrastructure.Clients.WriteThrough.Dtos;
using LeaderBoard.SharedKernel.Core.Extensions;
using Microsoft.Extensions.Options;

namespace LeaderBoard.Infrastructure.Clients.WriteThrough;

public class WriteThroughClient : IWriteThroughClient
{
    private readonly HttpClient _httpClient;
    private readonly IMapper _mapper;
    private readonly WriteThroughHttpClientOptions _options;

    public WriteThroughClient(
        HttpClient httpClient,
        IMapper mapper,
        IOptions<WriteThroughHttpClientOptions> options
    )
    {
        _httpClient = httpClient;
        _mapper = mapper;
        _options = options.Value;
    }

    public async Task AddPlayerScore(
        PlayerScoreDto playerScoreDto,
        CancellationToken cancellationToken = default
    )
    {
        // https://github.com/App-vNext/Polly#handing-return-values-and-policytresult
        var httpResponse = await _httpClient.PostAsJsonAsync(
            $"{_options.PlayersScoreEndpoint}/players/{playerScoreDto.PlayerId}",
            new AddPlayerScoreClientDto(
                playerScoreDto.Score,
                playerScoreDto.LeaderBoardName,
                playerScoreDto.Country,
                playerScoreDto.FirstName,
                playerScoreDto.LeaderBoardName
            ),
            cancellationToken
        );

        // https://stackoverflow.com/questions/21097730/usage-of-ensuresuccessstatuscode-and-handling-of-httprequestexception-it-throws
        // throw HttpResponseException instead of HttpRequestException (because we want detail response exception) with corresponding status code
        await httpResponse.EnsureSuccessStatusCodeWithDetailAsync();
    }

    public async Task UpdateScore(
        string playerId,
        double score,
        string leaderBoardName,
        CancellationToken cancellationToken = default
    )
    {
        // https://github.com/App-vNext/Polly#handing-return-values-and-policytresult
        var httpResponse = await _httpClient.PostAsJsonAsync(
            $"{_options.PlayersScoreEndpoint}/players/{playerId}",
            new UpdateScoreClientDto(score, leaderBoardName),
            cancellationToken
        );

        // https://stackoverflow.com/questions/21097730/usage-of-ensuresuccessstatuscode-and-handling-of-httprequestexception-it-throws
        // throw HttpResponseException instead of HttpRequestException (because we want detail response exception) with corresponding status code
        await httpResponse.EnsureSuccessStatusCodeWithDetailAsync();
    }

    public async Task DecrementScore(
        string leaderBoardName,
        string playerId,
        double decrementScore,
        CancellationToken cancellationToken = default
    )
    {
        // https://github.com/App-vNext/Polly#handing-return-values-and-policytresult
        var httpResponse = await _httpClient.PostAsJsonAsync(
            $"{_options.PlayersScoreEndpoint}/decrement/players/{playerId}",
            new DecrementScoreClientDto(decrementScore, leaderBoardName),
            cancellationToken
        );

        // https://stackoverflow.com/questions/21097730/usage-of-ensuresuccessstatuscode-and-handling-of-httprequestexception-it-throws
        // throw HttpResponseException instead of HttpRequestException (because we want detail response exception) with corresponding status code
        await httpResponse.EnsureSuccessStatusCodeWithDetailAsync();
    }

    public async Task IncrementScore(
        string leaderBoardName,
        string playerId,
        double incrementScore,
        CancellationToken cancellationToken = default
    )
    {
        // https://github.com/App-vNext/Polly#handing-return-values-and-policytresult
        var httpResponse = await _httpClient.PostAsJsonAsync(
            $"{_options.PlayersScoreEndpoint}/increment/players/{playerId}",
            new IncrementScoreClientDto(incrementScore, leaderBoardName),
            cancellationToken
        );

        // https://stackoverflow.com/questions/21097730/usage-of-ensuresuccessstatuscode-and-handling-of-httprequestexception-it-throws
        // throw HttpResponseException instead of HttpRequestException (because we want detail response exception) with corresponding status code
        await httpResponse.EnsureSuccessStatusCodeWithDetailAsync();
    }
}
