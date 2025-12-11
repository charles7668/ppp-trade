using CommunityToolkit.Mvvm.ComponentModel;
using ppp_trade.Enums;

namespace ppp_trade.Models;

public partial class MapHazardSetting : ObservableObject
{
    [ObservableProperty]
    private HazardLevel _hazardLevel = HazardLevel.SAFE;

    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _stat = string.Empty;
}