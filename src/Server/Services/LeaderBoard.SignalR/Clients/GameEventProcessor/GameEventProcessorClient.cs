using System.Text.Json;
using AutoMapper;
using LeaderBoard.SharedKernel.Core.Exceptions;
using LeaderBoard.SharedKernel.Core.Extensions;
using LeaderBoard.SignalR.Clients.GameEventProcessor.Dtos;
using LeaderBoard.SignalR.Dto;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

namespace LeaderBoard.SignalR.Clients.GameEventProcessor;

public class GameEventProcessorClient : IGameEventProcessorClient
{
    private readonly HttpClient _httpClient;
    private readonly IMapper _mapper;
    private readonly GameEventProcessorClientOptions _gameEventsClientOptions;

    public GameEventProcessorClient(
        HttpClient httpClient,
        IMapper mapper,
        IOptions<GameEventProcessorClientOptions> gameEventsClientOptions
    )
    {
        _httpClient = httpClient;
        _mapper = mapper;
        _gameEventsClientOptions = gameEventsClientOptions.Value;
    }

    public async Task<IList<PlayerScoreWithNeighborsDto>> GetPlayerGroupGlobalScoresAndRanks(
        IEnumerable<string> playerIds,
        string leaderBoardName,
        CancellationToken cancellationToken
    )
    {
        // https://stackoverflow.com/a/67877742/581476
        var qb = new QueryBuilder { { nameof(leaderBoardName), leaderBoardName }, { nameof(playerIds), playerIds }, };

        // https://github.com/App-vNext/Polly#handing-return-values-and-policytresult
        var httpResponse = await _httpClient.GetAsync(
            $"{_gameEventsClientOptions.PlayersScoreEndpoint}/players?{qb.ToQueryString().Value}",
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

    public async Task<PlayerScoreWithNeighborsDto?> GetGlobalScoreAndRank(
        string playerId,
        string leaderBoardName,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // https://stackoverflow.com/a/67877742/581476
            var qb = new QueryBuilder { { nameof(leaderBoardName), leaderBoardName }, };

            // https://github.com/App-vNext/Polly#handing-return-values-and-policytresult
            var httpResponse = await _httpClient.GetAsync(
                $"{_gameEventsClientOptions.PlayersScoreEndpoint}/players/{playerId}?{qb.ToQueryString().Value}",
                cancellationToken
            );

            // https://stackoverflow.com/questions/21097730/usage-of-ensuresuccessstatuscode-and-handling-of-httprequestexception-it-throws
            // throw HttpResponseException instead of HttpRequestException (because we want detail response exception) with corresponding status code
            await httpResponse.EnsureSuccessStatusCodeWithDetailAsync();

            var playerScoreClientDto = await httpResponse.Content.ReadFromJsonAsync<PlayerScoreWithNeighborsClientDto>(
                cancellationToken: cancellationToken
            );

            var dto = _mapper.Map<PlayerScoreWithNeighborsDto>(playerScoreClientDto);

            return dto;
        }
        catch (HttpResponseException e)
        {
            if (e.StatusCode == StatusCodes.Status404NotFound)
                return null;

            throw;
        }
    }
}
