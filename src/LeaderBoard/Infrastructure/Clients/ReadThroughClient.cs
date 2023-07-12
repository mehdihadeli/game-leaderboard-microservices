using AutoMapper;
using LeaderBoard.Dtos;
using LeaderBoard.SharedKernel.Core.Extensions;
using Microsoft.Extensions.Options;

namespace LeaderBoard.Infrastructure.Clients;

public class ReadThroughClient : IReadThroughClient
{
    private readonly HttpClient _httpClient;
    private readonly IMapper _mapper;
    private readonly ReadThroughHttpClientOptions _readThroughHttpClientOptions;

    public ReadThroughClient(
        HttpClient httpClient,
        IMapper mapper,
        IOptions<ReadThroughHttpClientOptions> readThroughHttpClientOptions
    )
    {
        _httpClient = httpClient;
        _mapper = mapper;
        _readThroughHttpClientOptions = readThroughHttpClientOptions.Value;
    }

    public async Task<List<PlayerScoreDto>?> GetRangeScoresAndRanks(
        string leaderBoardName,
        int start,
        int end,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    )
    {
        // https://github.com/App-vNext/Polly#handing-return-values-and-policytresult
        var httpResponse = await _httpClient.GetAsync(
            $"{_readThroughHttpClientOptions.PlayersScoreEndpoint}?leaderBoardName={leaderBoardName}&start={start}&end={end}&isDesc={isDesc}",
            cancellationToken
        );

        // https://stackoverflow.com/questions/21097730/usage-of-ensuresuccessstatuscode-and-handling-of-httprequestexception-it-throws
        // throw HttpResponseException instead of HttpRequestException (because we want detail response exception) with corresponding status code
        await httpResponse.EnsureSuccessStatusCodeWithDetailAsync();

        var playerScoreDtos = await httpResponse.Content.ReadFromJsonAsync<List<PlayerScoreDto>>(
            cancellationToken: cancellationToken
        );

        return playerScoreDtos;
    }

    public async Task<PlayerScoreDto?> GetGlobalScoreAndRank(
        string leaderBoardName,
        string playerId,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    )
    {
        // https://github.com/App-vNext/Polly#handing-return-values-and-policytresult
        var httpResponse = await _httpClient.GetAsync(
            $"{_readThroughHttpClientOptions.PlayersScoreEndpoint}/{playerId}?leaderBoardName={leaderBoardName}&isDesc={isDesc}",
            cancellationToken
        );

        // https://stackoverflow.com/questions/21097730/usage-of-ensuresuccessstatuscode-and-handling-of-httprequestexception-it-throws
        // throw HttpResponseException instead of HttpRequestException (because we want detail response exception) with corresponding status code
        await httpResponse.EnsureSuccessStatusCodeWithDetailAsync();

        var playerScoreDto = await httpResponse.Content.ReadFromJsonAsync<PlayerScoreDto>(
            cancellationToken: cancellationToken
        );

        return playerScoreDto;
    }

    public async Task<List<PlayerScoreDto>?> GetPlayerGroupScoresAndRanks(
        string leaderBoardName,
        IEnumerable<string> playerIds,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    )
    {
        // https://github.com/App-vNext/Polly#handing-return-values-and-policytresult
        var httpResponse = await _httpClient.GetAsync(
            $"{_readThroughHttpClientOptions.PlayersScoreEndpoint}/by-group?leaderBoardName={leaderBoardName}&playerIds={playerIds}&isDesc={isDesc}",
            cancellationToken
        );

        // https://stackoverflow.com/questions/21097730/usage-of-ensuresuccessstatuscode-and-handling-of-httprequestexception-it-throws
        // throw HttpResponseException instead of HttpRequestException (because we want detail response exception) with corresponding status code
        await httpResponse.EnsureSuccessStatusCodeWithDetailAsync();

        var playerScoreDtos = await httpResponse.Content.ReadFromJsonAsync<List<PlayerScoreDto>>(
            cancellationToken: cancellationToken
        );

        return playerScoreDtos;
    }
}
