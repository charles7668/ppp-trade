using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveCharts;
using ppp_trade.Enums;

namespace ppp_trade.ViewModels;

public class ExchangeRateVM
{
    public string? CurrencyImageUrl { get; set; }

    public double Value { get; set; }
}

public class MatchedCurrencyVM
{
    public string? DetailsId { get; set; }

    public string? MatchedCurrencyImage { get; set; }

    public string? PayCurrencyImage { get; set; }

    public List<ExchangeRateVM> ExchangeRateList { get; set; } = [];

    public SeriesCollection SeriesCollection { get; set; } = [];

    public ObservableCollection<string> Labels { get; set; } = [];

    public Func<double, string>? YFormatter { get; set; }
}

public class MatchedItemVM
{
    public int Count { get; set; }

    public string? MatchedItemImage { get; set; }

    public string? QueryId { get; set; }

    public List<PriceAnalysisVM> PriceAnalysisVMs { get; set; } = [];
}

public partial class ItemStatVM : ObservableObject
{
    [ObservableProperty]
    private HazardLevel _hazardLevel = HazardLevel.SAFE;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private int? _maxValue;

    [ObservableProperty]
    private int? _minValue;

    public string? Id { get; set; }

    public string? Type { get; set; }

    public string? StatText { get; set; }
}

public class PriceAnalysisVM
{
    public string? Currency { get; set; }

    public double Price { get; set; }

    public string? CurrencyImageUrl { get; set; }

    public int Count { get; set; }
}

public class Poe2ItemVM
{
    public string? ItemName { get; set; }

    public int? ItemLevelMin { get; set; }

    public int? ItemLevelMax { get; set; }

    public bool FilterItemLevel { get; set; } = true;

    public bool FilterRarity { get; set; } = true;

    public bool FilterItemBase { get; set; } = true;

    public string? Rarity { get; set; }

    public string? ItemBaseName { get; set; }

    public bool FilterRunSockets { get; set; }

    public int? RunSocketsMin { get; set; }

    public int? RunSocketsMax { get; set; }

    public List<ItemStatVM> StatVMs { get; set; } = [];
}

public class Poe1ItemVM
{
    public string? ItemName { get; set; }

    public int? ItemLevelMin { get; set; }

    public int? ItemLevelMax { get; set; }

    public bool FilterItemLevel { get; set; } = true;

    public bool FilterRarity { get; set; } = true;

    public bool FilterLink { get; set; } = true;

    public bool FilterGemLevel { get; set; }

    public bool FilterItemBase { get; set; } = true;

    public string? Rarity { get; set; }

    public int? LinkCountMin { get; set; }

    public int? LinkCountMax { get; set; }

    public int? GemLevelMin { get; set; }

    public int? GemLevelMax { get; set; }

    public string? ItemBaseName { get; set; }

    public YesNoAnyOption FoulBorn { get; set; } = YesNoAnyOption.ANY;

    public List<ItemStatVM> StatVMs { get; set; } = [];
}