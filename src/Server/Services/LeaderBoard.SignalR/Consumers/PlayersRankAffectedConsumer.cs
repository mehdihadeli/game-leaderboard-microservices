using LeaderBoard.SharedKernel.Application.Messages;
using LeaderBoard.SignalR.Hubs;
using MassTransit;

namespace LeaderBoard.SignalR.Consumers;

public class PlayersRankAffectedConsumer : IConsumer<PlayersRankAffected>
{
    private readonly IHubService _hubService;

    public PlayersRankAffectedConsumer(IHubService hubService)
    {
        _hubService = hubService;
    }

    public async Task Consume(ConsumeContext<PlayersRankAffected> context)
    {
        await _hubService.UpdatePlayersScoreForClient(context.Message.PlayerIds);
    }
}
