using AutoMapper;
using LeaderBoard.SignalR.Clients.GameEventProcessor.Dtos;
using LeaderBoard.SignalR.Dto;

namespace LeaderBoard.SignalR;

public class Mapping : Profile
{
    public Mapping()
    {
        CreateMap<PlayerScoreClientDto, PlayerScoreDto>();
        CreateMap<PlayerScoreWithNeighborsClientDto, PlayerScoreWithNeighborsDto>();
    }
}
