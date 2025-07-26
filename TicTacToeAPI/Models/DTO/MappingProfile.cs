using AutoMapper;
using Newtonsoft.Json;

namespace TicTacToeAPI.Models.DTO
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Game, GameResponse>()
            .ForMember(dest => dest.Board,
                       opt => opt.MapFrom(game => JsonConvert.DeserializeObject<char[][]>(game.Board)));
        } 
    }
}
