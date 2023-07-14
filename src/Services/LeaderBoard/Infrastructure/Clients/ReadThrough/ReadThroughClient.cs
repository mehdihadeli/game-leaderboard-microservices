using AutoMapper;
using LeaderBoard.Dtos;
using LeaderBoard.Infrastructure.Clients.Dtos;
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
            $"{_readThroughHttpClientOptions.PlayersScoreEndpoint}/range?leaderBoardName={leaderBoardName}&start={start}&end={end}&isDesc={isDesc}",
            cancellationToken
        );

        // https://stackoverflow.com/questions/21097730/usage-of-ensuresuccessstatuscode-and-handling-of-httprequestexception-it-throws
        // throw HttpResponseException instead of HttpRequestException (because we want detail response exception) with corresponding status code
        await httpResponse.EnsureSuccessStatusCodeWithDetailAsync();

        var playerScoreClientDtos = await httpResponse.Content.ReadFromJsonAsync<
            List<PlayerScoreClientDto>
        >(cancellationToken: cancellationToken);

        var dtos = _mapper.Map<List<PlayerScoreDto>>(playerScoreClientDtos);

        return dtos;
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
            $"{_readThroughHttpClientOptions.PlayersScoreEndpoint}/players/{playerId}?leaderBoardName={leaderBoardName}&isDesc={isDesc}",
            cancellationToken
        );

        // https://stackoverflow.com/questions/21097730/usage-of-ensuresuccessstatuscode-and-handling-of-httprequestexception-it-throws
        // throw HttpResponseException instead of HttpRequestException (because we want detail response exception) with corresponding status code
        await httpResponse.EnsureSuccessStatusCodeWithDetailAsync();

        var playerScoreDto = await httpResponse.Content.ReadFromJsonAsync<PlayerScoreClientDto>(
            cancellationToken: cancellationToken
        );

        var dto = _mapper.Map<PlayerScoreDto>(playerScoreDto);

        return dto;
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
            $"{_readThroughHttpClientOptions.PlayersScoreEndpoint}/players?leaderBoardName={leaderBoardName}&playerIds={playerIds}&isDesc={isDesc}",
            cancellationToken
        );

        // https://stackoverflow.com/questions/21097730/usage-of-ensuresuccessstatuscode-and-handling-of-httprequestexception-it-throws
        // throw HttpResponseException instead of HttpRequestException (because we want detail response exception) with corresponding status code
        await httpResponse.EnsureSuccessStatusCodeWithDetailAsync();

        var playerScoreClientDtos = await httpResponse.Content.ReadFromJsonAsync<
            List<PlayerScoreClientDto>
        >(cancellationToken: cancellationToken);

        var dtos = _mapper.Map<List<PlayerScoreDto>>(playerScoreClientDtos);

        return dtos;
    }
}
