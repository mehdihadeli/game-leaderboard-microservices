using AutoMapper;
using LeaderBoard.Dtos;
using LeaderBoard.Models;

namespace LeaderBoard;

public class Mappings : Profile
{
    public Mappings()
    {
        CreateMap<PlayerScore, PlayerScoreDto>();
    }
}
