using ppp_trade.Enums;

namespace ppp_trade.Models;

public class SearchRequest
{
    public ServerOption ServerOption { get; set; }

    public CorruptedState CorruptedState { get; set; }

    public CollapseByAccount CollapseByAccount { get; set; }

    public string? TradeType { get; set; }

    public Item? Item { get; set; }

    public string? ItemName { get; set; }

    public int? ItemLevelMin { get; set; }

    public int? ItemLevelMax { get; set; }

    public bool FilterItemLevel { get; set; } = true;

    public bool FilterRarity { get; set; } = true;

    public bool FilterLink { get; set; } = true;

    public bool FilterItemBase { get; set; }

    public string? Rarity { get; set; }

    public int? LinkCountMin { get; set; }

    public int? LinkCountMax { get; set; }

    public string? ItemBase { get; set; }

    public YesNoAnyOption FoulBorn { get; set; } = YesNoAnyOption.ANY;

    public List<StatFilter> Stats { get; set; } = [];
}

public class StatFilter
{
    public bool Disabled { get; set; }

    public int? MinValue { get; set; }

    public int? MaxValue { get; set; }

    public string? StatId { get; set; }
}