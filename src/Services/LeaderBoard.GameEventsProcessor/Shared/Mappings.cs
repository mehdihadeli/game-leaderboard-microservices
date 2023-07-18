using AutoMapper;
using LeaderBoard.GameEventsProcessor.PlayerScores.Dtos;
using LeaderBoard.GameEventsProcessor.Shared.Clients.ReadThrough.Dtos;
using LeaderBoard.SharedKernel.Application.Models;

namespace LeaderBoard.GameEventsProcessor.Shared;

public class Mappings : Profile
{
    public Mappings()
    {
        CreateMap<PlayerScoreReadModel, PlayerScoreDto>();
        CreateMap<PlayerScoreDto, PlayerScoreClientDto>();
        CreateMap<PlayerScoreWithNeighborsDto, PlayerScoreWithNeighborsClientDto>();
        CreateMap<PlayerScoreWithNeighborsClientDto, PlayerScoreWithNeighborsDto>();
        CreateMap<PlayerScoreClientDto, PlayerScoreDto>();
    }
}
