using AutoMapper;
using LeaderBoard.Dtos;
using LeaderBoard.Infrastructure.Clients.Dtos;
using LeaderBoard.SharedKernel.Application.Models;

namespace LeaderBoard;

public class Mappings : Profile
{
    public Mappings()
    {
        CreateMap<PlayerScore, PlayerScoreDto>();
        CreateMap<PlayerScoreDto, PlayerScoreClientDto>();
        CreateMap<PlayerScoreClientDto, PlayerScoreDto>();
    }
}
