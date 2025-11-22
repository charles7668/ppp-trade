using AutoMapper;
using ppp_trade.Models;
using ppp_trade.ViewModels;

namespace ppp_trade.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ItemStat, MainWindowViewModel.ItemStatVM>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Stat.Id))
            .ForMember(dest => dest.StatText, opt => opt.MapFrom(src => src.Stat.Text))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Stat.Type))
            .ForMember(dest => dest.MinValue, opt => opt.MapFrom(src => src.Value));

        CreateMap<Item, MainWindowViewModel.ItemVM>()
            .ForMember(dest => dest.StatVMs, opt => opt.MapFrom(src => src.Stats));
    }
}