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

        CreateMap<Poe1Item, MainWindowViewModel.ItemVM>()
            .ForMember(dest => dest.ItemLevelMin,
                opt => opt.MapFrom(src => src.ItemLevel == 0 ? null : (int?)src.ItemLevel))
            .ForMember(dest => dest.LinkCountMin,
                opt => opt.MapFrom(src => src.Link == 0 ? null : (int?)src.Link))
            .ForMember(dest => dest.ItemName, opt => opt.MapFrom(src => src.ItemName + " " + src.ItemBaseName))
            .ForMember(dest => dest.StatVMs, opt => opt.MapFrom(src => src.Stats));

        CreateMap<MainWindowViewModel.ItemVM, SearchRequest>()
            .ForMember(dest => dest.Stats, opt => opt.MapFrom(src => src.StatVMs));
        CreateMap<MainWindowViewModel.ItemStatVM, StatFilter>()
            .ForMember(dest => dest.Disabled, opt =>
                opt.MapFrom(src => !src.IsSelected))
            .ForMember(dest => dest.MinValue,
                opt => opt.MapFrom(src => src.MinValue))
            .ForMember(dest => dest.MaxValue,
                opt => opt.MapFrom(src => src.MaxValue))
            .ForMember(dest => dest.StatId,
                opt => opt.MapFrom(src => src.Id));
    }
}