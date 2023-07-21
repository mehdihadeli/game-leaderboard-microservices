using AutoMapper;
using LeaderBoard.GameEventsProcessor.PlayerScores.Dtos;
using LeaderBoard.GameEventsProcessor.Shared.Clients.ReadThrough.Dtos;
using LeaderBoard.SharedKernel.Core.Extensions;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

namespace LeaderBoard.GameEventsProcessor.Shared.Clients.ReadThrough;

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
        // https://stackoverflow.com/a/67877742/581476
        var qb = new QueryBuilder
                 {
                     { nameof(start), start.ToString() },
                     { nameof(end), end.ToString() },
                     { nameof(leaderBoardName), leaderBoardName },
                     { nameof(isDesc), isDesc.ToString() },
                 };

        // https://github.com/App-vNext/Polly#handing-return-values-and-policytresult
        var httpResponse = await _httpClient.GetAsync(
            $"{_readThroughHttpClientOptions.PlayersScoreEndpoint}/range?{qb.ToQueryString().Value}",
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

    public async Task<PlayerScoreWithNeighborsDto?> GetGlobalScoreAndRank(
        string leaderBoardName,
        string playerId,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    )
    {
        // https://stackoverflow.com/a/67877742/581476
        var qb = new QueryBuilder
        {
            { nameof(leaderBoardName), leaderBoardName },
            { nameof(isDesc), isDesc.ToString() },
        };

        // https://github.com/App-vNext/Polly#handing-return-values-and-policytresult
        var httpResponse = await _httpClient.GetAsync(
            $"{_readThroughHttpClientOptions.PlayersScoreEndpoint}/players/{playerId}?{qb.ToQueryString().Value}",
            cancellationToken
        );

        // https://stackoverflow.com/questions/21097730/usage-of-ensuresuccessstatuscode-and-handling-of-httprequestexception-it-throws
        // throw HttpResponseException instead of HttpRequestException (because we want detail response exception) with corresponding status code
        await httpResponse.EnsureSuccessStatusCodeWithDetailAsync();

        var playerScoreDto =
            await httpResponse.Content.ReadFromJsonAsync<PlayerScoreWithNeighborsClientDto>(
                cancellationToken: cancellationToken
            );

        var dto = _mapper.Map<PlayerScoreWithNeighborsDto>(playerScoreDto);

        return dto;
    }

    public async Task<List<PlayerScoreWithNeighborsDto>?> GetPlayerGroupGlobalScoresAndRanks(
        string leaderBoardName,
        IEnumerable<string> playerIds,
        bool isDesc = true,
        CancellationToken cancellationToken = default
    )
    {
        // https://stackoverflow.com/a/67877742/581476
        var qb = new QueryBuilder
        {
            { nameof(leaderBoardName), leaderBoardName },
            { nameof(isDesc), isDesc.ToString() },
            { nameof(playerIds), playerIds },
        };

        // https://github.com/App-vNext/Polly#handing-return-values-and-policytresult
        var httpResponse = await _httpClient.GetAsync(
            $"{_readThroughHttpClientOptions.PlayersScoreEndpoint}/players?{qb.ToQueryString().Value}",
            cancellationToken
        );

        // https://stackoverflow.com/questions/21097730/usage-of-ensuresuccessstatuscode-and-handling-of-httprequestexception-it-throws
        // throw HttpResponseException instead of HttpRequestException (because we want detail response exception) with corresponding status code
        await httpResponse.EnsureSuccessStatusCodeWithDetailAsync();

        var playerScoreClientDtos = await httpResponse.Content.ReadFromJsonAsync<
            List<PlayerScoreWithNeighborsClientDto>
        >(cancellationToken: cancellationToken);

        var dtos = _mapper.Map<List<PlayerScoreWithNeighborsDto>>(playerScoreClientDtos);

        return dtos;
    }
}
