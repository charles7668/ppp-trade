
using CommunityToolkit.Mvvm.ComponentModel;

namespace ppp_trade.Models;

public partial class RegexSetting : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _regex = string.Empty;
}
