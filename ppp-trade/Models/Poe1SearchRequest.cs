using ppp_trade.Enums;

namespace ppp_trade.Models;

public class SearchRequestBase
{
    public ItemBase? Item { get; set; }

    public ServerOption ServerOption { get; set; }

    public CorruptedState CorruptedState { get; set; }

    public CollapseByAccount CollapseByAccount { get; set; }

    public string? TradeType { get; set; }

    public string? ItemName { get; set; }

    public int? ItemLevelMin { get; set; }

    public int? ItemLevelMax { get; set; }

    public bool FilterItemLevel { get; set; } = true;

    public bool FilterRarity { get; set; } = true;

    public string? Rarity { get; set; }

    public bool FilterItemBase { get; set; }

    public string? ItemBase { get; set; }

    public List<StatFilter> Stats { get; set; } = [];
}

public class Poe1SearchRequest : SearchRequestBase
{
    public bool FilterLink { get; set; } = true;


    public int? LinkCountMin { get; set; }

    public int? LinkCountMax { get; set; }


    public YesNoAnyOption FoulBorn { get; set; } = YesNoAnyOption.ANY;
}

public class Poe2SearchRequest : SearchRequestBase
{
    public int? RuneSockets { get; set; }
}